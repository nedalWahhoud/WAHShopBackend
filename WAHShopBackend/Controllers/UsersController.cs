using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using WAHShopBackend.EmailF;
using Microsoft.AspNetCore.Identity;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(MyDbContext context, IOptions<JwtSettings> jwtSettings, EmailService emailService, UserManager<UserIdentity> userManager) : ControllerBase
    {
        private readonly IOptions<JwtSettings> _jwtSettings = jwtSettings;
        private readonly MyDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        private UserManager<UserIdentity> _userManager = userManager;

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            try
            {
                if (loginModel.SignupProvider == "Manual")
                {
                    // es konnte dass bei login die UserName oder Email verwendet werden kann
                    var user = await _context.Users.FirstOrDefaultAsync(u =>
                       u.UserName!.ToLower() == loginModel.UserName!.ToLower()
                       || u.Email!.ToLower() == loginModel.Email!.ToLower()
                       || u.Email!.ToLower() == loginModel.UserName!.ToLower() 
                       && u.IsGuest == false && u.SignupProvider == "Manual");

                    if (user == null || !BCrypt.Net.BCrypt.Verify(loginModel!.Password.Trim(), user!.Password!.Trim()) || user.IsGuest == true)
                    {
                        return Unauthorized(new ValidationResult { Result = false, Message = "Invalid credentials" });
                    }
                    if (user.IsAktiv == false)
                    {
                        return Unauthorized(new ValidationResult { Result = false, Message = "Benutzer ist nicht aktiv" });
                    }

                    return GetToken(user);
                }
                else if (loginModel.SignupProvider == "Google")
                {
                    // Google login
                    // es konnte dass bei login die UserName oder Email verwendet werden kann
                    var user = await _context.Users.FirstOrDefaultAsync(u =>
                       u.UserName!.ToLower() == loginModel.UserName!.ToLower()
                       || u.Email!.ToLower() == loginModel.Email!.ToLower()
                       || u.Email!.ToLower() == loginModel.UserName!.ToLower()
                       && u.IsGuest == false && u.SignupProvider == "Google");

                    // wenn der User nicht existiert, dann signup ohne Passwort und bestätigungslink da es ein Google Login ist
                    if (user == null)
                    {
                        User newUser = new()
                        {
                            UserName = loginModel.UserName,
                            Password = "",
                            Email = loginModel.Email,
                            BirthDate = "0",
                            Role = "user", // Standardrolle für Google-Login
                            IsGuest = false,
                            IsAktiv = true, // Google login ist immer aktiv
                            SignupProvider = "Google"
                        };

                        _context.Users.Add(newUser);
                        var result = await _context.SaveChangesAsync();
                        if (result > 0)
                        {
                            return GetToken(newUser);
                        }
                        else
                        {
                            return BadRequest(new ValidationResult { Result = false, Message = "Fehler beim Erstellen des Benutzers" });
                        }
                    }
                    else if (user.IsAktiv == false)
                    {
                        return Unauthorized(new ValidationResult { Result = false, Message = "Benutzer ist nicht aktiv" });
                    }
                    return GetToken(user);
                }
                else
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Ungültiger Anmeldeanbieter" });
                }
            }
            catch
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
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

            if (user == null) return Unauthorized(new ValidationResult { Result = false, Message = "Benutzer nicht gefunden"} );

            if (user.IsAktiv == false)
            {
                return Unauthorized(new ValidationResult { Result = false, Message = "Benutzer ist nicht aktiv" });
            }

            return user;
        }
        [HttpPost("signup")]
        public async Task<ActionResult<User>> Signup(SignupModel signupModel)
        {
            try
            {
                // if user name exist
                if (await _context.Users.AnyAsync(u => u.UserName.ToLower() == signupModel.UserName.ToLower()))
                {
                    return BadRequest(new ValidationResult {Result = false , Message = "Benutzername bereits existiert" });
                }
                // if user email exist
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == signupModel.Email.ToLower()))
                {
                    return BadRequest(new ValidationResult {Result = false, Message = "Email bereits existiert" });
                }
                // password check
                if (signupModel.Password != signupModel.PasswordAgain)
                {
                    return BadRequest(new ValidationResult{ Result = false, Message = "Passwörter stimmen nicht überein" });
                }
                else if (string.IsNullOrEmpty(signupModel.UserName) || string.IsNullOrEmpty(signupModel.Password) || string.IsNullOrEmpty(signupModel.Email))
                {
                    return BadRequest(new { error = "Benutzername, Passwort und Email sind erforderlich" });
                }

                User user = new()
                {
                    UserName = signupModel.UserName,
                    Password = BCrypt.Net.BCrypt.HashPassword(signupModel.Password.Trim()),
                    Email = signupModel.Email,
                    Role = "user",
                    BirthDate = signupModel.BirthDate,
                    IsAktiv = false,
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Send confirmation email
                UserIdentity userIdentity = new UserIdentity()
                {
                    Id = user.Id.ToString(),
                    UserName = user.UserName,
                    Email = user.Email,
                };
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(userIdentity);
                var result = await _emailService.SendEmailConfirmationAsync(user, token);
                if (!result.Result)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Bestätigungs-E-Mail konnte nicht gesendet werden" });
                }
                else
                {
                    return Ok(new ValidationResult { Result = true, Message = "Benutzer erfolgreich erstellt. Bitte prüf mal Ihre Email um Ihre Konto zu bestätigen" });
                }
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new ValidationResult{ Result = false, Message = "Internal server error" });
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
        [HttpPut("userActivate")]
        public async Task <IActionResult> UserActivate([FromBody] ActivateRequest activateRequest)
        {
            try
            {
                var userId = activateRequest.UserId;
                var token = activateRequest.Token;

                if (string.IsNullOrEmpty(activateRequest.UserId))
                    return BadRequest(new ValidationResult { Result = false, Message = "UserId ist null oder leer." });

                if (!int.TryParse(activateRequest.UserId, out _))
                    return BadRequest(new ValidationResult { Result = false, Message = "UserId ist nicht eine gültige Nummer." });

                int userIdInt = int.Parse(activateRequest.UserId);

                // activate user in database
                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Benutzer nicht gefunden" });
                }
                if (user.IsAktiv)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Benutzer ist bereits aktiv" });
                }
                user.IsAktiv = true;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = true, Message = "Benutzer erfolgreich aktiviert" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
