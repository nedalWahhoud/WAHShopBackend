using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using System.Net;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(MyDbContext context, IOptions<JwtSettings> jwtSettings) : ControllerBase
    {
        private readonly IOptions<JwtSettings> _jwtSettings = jwtSettings;
        private readonly MyDbContext _context = context;

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            try
            {
                // es konnte dass bei
                // login die UserName oder Email verwendet werden kann
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                   u.UserName!.ToLower() == loginModel.UserName!.ToLower()
                   || u.Email!.ToLower() == loginModel.Email!.ToLower()
                   || u.Email!.ToLower() == loginModel.UserName!.ToLower());

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginModel!.Password.Trim(), user!.Password!.Trim()) || user.IsGuest == true)
                {
                    return Unauthorized(new { error = "Invalid credentials" });
                }
                return GetToken(user);
            }
            catch
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
        private ObjectResult GetToken(User user)
        {
            var claims = new[]
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Name, user.UserName!),
               new Claim(ClaimTypes.Email, user.Email!),
               new Claim(ClaimTypes.Role, user.Role!),
               new Claim(ClaimTypes.DateOfBirth, user.BirthDate)
             };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Key!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }
        // get: api/users
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return user;
        }
        [HttpPost("signup")]
        public async Task<ActionResult<User>> Signup(SignupModel signupModel)
        {
            // if user name exist
            if (await _context.Users.AnyAsync(u => u.UserName.ToLower() == signupModel.UserName.ToLower()))
            {
                return BadRequest(new { error = "Username already exists" });
            }
            // if user email exist
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == signupModel.Email.ToLower()))
            {
                return BadRequest(new { error = "Email already exists" });
            }
            // password check
            if (signupModel.Password != signupModel.PasswordAgain)
            {
                return BadRequest(new { error = "Passwords do not match" });
            }
            else if (string.IsNullOrEmpty(signupModel.UserName) || string.IsNullOrEmpty(signupModel.Password) || string.IsNullOrEmpty(signupModel.Email))
            {
                return BadRequest(new { error = "UserName, PasswordHash and Email are required" });
            }

            try
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == signupModel.UserName || u.Email == signupModel.Email);
                if (existingUser != null)
                {
                    return Conflict(new { error = "User or email already exists" });
                }
                User user = new()
                {
                    UserName = signupModel.UserName,
                    Password = BCrypt.Net.BCrypt.HashPassword(signupModel.Password.Trim()),
                    Email = signupModel.Email,
                    Role = "user",
                    BirthDate = signupModel.BirthDate
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // get the UserId to send with user info
                int id = await _context.Users
                    .Where(u => u.UserName == signupModel.UserName || u.Email == signupModel.Email)
                    .Select(u => u.Id).FirstOrDefaultAsync();
                user.Id = id;


                return GetToken(user);
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser(UpdateProfile updateProfile)
        {
            if (updateProfile == null || updateProfile.UserId <= 0)
            {
                return BadRequest(new ValidationResult { Result = false,  Message = "Invalid user data" });
            }
            var existingUser = await _context.Users.FindAsync(updateProfile.UserId);
            if (existingUser == null)
            {
                return NotFound(new ValidationResult { Result = false, Message = "User not found" });
            }

            bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(updateProfile.OldPassword, existingUser.Password);
            if (!isOldPasswordCorrect)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Das alte Passwort ist nicht korrekt." });
            }

            // ✅ تأكد أن الباسورد الجديد مختلف عن القديم
            bool isSameAsOld = BCrypt.Net.BCrypt.Verify(updateProfile.NewPassword, existingUser.Password);
            if (isSameAsOld)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Das neue Passwort darf nicht gleich dem alten sein." });
            }
            try
            {             
                // ✅ تحديث الباسورد الجديد بعد التشفير
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(updateProfile.NewPassword);

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return Ok(new ValidationResult { Result = true, Message = "Passwort wurde erfolgreich geändert." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
            }
        }
        [HttpPost("addGuest")]
        public async Task<IActionResult> AddGuest(User userGuest)
        {
            // if user name exist
            if (await _context.Users.AnyAsync(u => u.UserName.ToLower() == userGuest.UserName.ToLower()))
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Username already exists" });
            }
            // if user email exist
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == userGuest.Email.ToLower()))
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Email already exists" });
            }
            // empty check
            if (string.IsNullOrEmpty(userGuest.UserName) || string.IsNullOrEmpty(userGuest.Password) || string.IsNullOrEmpty(userGuest.Email))
            {
                return BadRequest(new ValidationResult { Result = false, Message = "UserName, PasswordHash and Email are required" });
            }

            try
            {
                User guest = new()
                {
                    UserName = userGuest.UserName,
                    Password = BCrypt.Net.BCrypt.HashPassword(userGuest.Password),
                    Email = userGuest.Email,
                    Role = "user",
                    BirthDate = DateTime.Now.ToString("yyyy.MM.dd"),
                    IsGuest = true
                };
                _context.Users.Add(guest);
                await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = true, Message = $"Id:{guest.Id}" });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
            }
        }
    }
}
