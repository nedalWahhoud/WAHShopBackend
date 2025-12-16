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
using System.Reflection.Emit;

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
                        u.Email!.ToLower() == loginModel.Email!.ToLower()
                       && u.IsGuest == false && u.SignupProvider == "Manual" && u.IsAktiv == true);

                    if (user == null || !BCrypt.Net.BCrypt.Verify(loginModel!.Password.Trim(), user!.Password!.Trim()) || user.IsGuest == true)
                    {
                        return Unauthorized(new ValidationResult { Result = false, Message = "Falsches Passwort oder E-Mail." });
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
                        u.Email!.ToLower() == loginModel.Email!.ToLower()
                       && u.IsGuest == false && u.SignupProvider == "Google" && u.IsAktiv == true);

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
            catch(Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error " + ex.Message });
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
               new Claim(ClaimTypes.DateOfBirth, user.BirthDate),
               new Claim("SignupProvider", user.SignupProvider)
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
              /*  var checkUsername = await _context.Users.AnyAsync(u => u.UserName.ToLower() == signupModel.UserName.ToLower() && u.IsGuest == false);
                if (checkUsername)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Benutzername bereits existiert" });
                }*/
                // if user email exist
                var checkEmail = await _context.Users.AnyAsync(u => u.Email.ToLower() == signupModel.Email.ToLower() && u.IsGuest == false);
                if (checkEmail)
                {
                    // check wenn die user inaktiv ist dann sende die bestätigungs email nochmal
                   var user1 = await _context.Users.FirstOrDefaultAsync(u =>
                   u.Email!.ToLower() == signupModel.Email!.ToLower()
                   && u.IsGuest == false && u.SignupProvider == "Manual" && u.IsAktiv == false);
                    if (user1 != null)
                    {
                        // Send confirmation email
                        UserIdentity userIdentity = new UserIdentity()
                        {
                            Id = user1.Id.ToString(),
                            UserName = user1.UserName,
                            Email = user1.Email,
                        };
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(userIdentity);
                        var result = await _emailService.SendEmailConfirmationAsync(user1, token);
                        if (!result.Result)
                        {
                            return BadRequest(new ValidationResult { Result = false, Message = "Bestätigungs-E-Mail konnte nicht gesendet werden" });
                        }
                        else
                        {
                            return Ok(new ValidationResult { Result = true, Message = "SignupSuccessMessageAgain" });
                        }
                    }


                return BadRequest(new ValidationResult { Result = false, Message = "Email bereits existiert" });
                }
                // password check
                if (signupModel.Password != signupModel.PasswordAgain)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Passwörter stimmen nicht überein" });
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
                    SignupProvider = signupModel.SignupProvider
                };

                _context.Users.Add(user);
                var addResult = await _context.SaveChangesAsync();
                if (addResult > 0)
                {
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
                        return Ok(new ValidationResult { Result = true, Message = "SignupSuccessMessage" });
                    }
                }
                else
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Fehler beim Erstellen des Benutzers" });
                }
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
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

            try
            {   // if update password
                if (updateProfile.UpdateType == UpdateTypeEnum.Password)
                {
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
                    // ✅ تحديث الباسورد الجديد بعد التشفير
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(updateProfile.NewPassword);
                }
                else if (updateProfile.UpdateType == UpdateTypeEnum.Birthday)
                {
                    existingUser.BirthDate = updateProfile.BirthDate;
                }


                _context.Users.Update(existingUser);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return GetToken(existingUser);
                }
                else
                { 
                    return BadRequest(new ValidationResult { Result = false, Message = "könnte nicht in die Databank updaten" }); 
                }
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
           /* var checkUsername = await _context.Users.AnyAsync(u => u.UserName.ToLower() == userGuest.UserName.ToLower() && u.IsGuest==false);
            if (checkUsername)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Username already exists" });
            }*/
            // if user email exist
            var checkEmail = await _context.Users.AnyAsync(u => u.Email.ToLower() == userGuest.Email.ToLower() && u.IsGuest == false);
            if (checkEmail)
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
                    BirthDate = userGuest.BirthDate,
                    IsGuest = true,
                    IsAktiv = false, // Guest accounts are not active by default
                    SignupProvider = "Guest"
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
        [HttpGet("checkPassword")]
        public async Task<IActionResult> CheckPassword(int userId, string password)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Benutzer nicht gefunden" });
                }
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
                if (isPasswordValid)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Passwort ist korrekt" });
                }
                else
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Passwort ist falsch" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("checkAdminStatus/{id}")]
        public async Task<IActionResult> CheckAdminStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new ValidationResult { Result = false, Message = "nicht gefunden" });
            }
            if (user.Role == "admin")
            {
                return Ok(new ValidationResult { Result = true, Message = "Benutzer ist ein Admin" });
            }
            else
            {
                return BadRequest(new ValidationResult { Result = false, Message = "kein Admin" });
            }
        }
        [HttpDelete("accountDelete/{userId}")]
        public async Task<IActionResult> AccountDelete(int userId)
        {
            var users = _context.Users.Where(a => a.Id == userId);

            // check if user exist 
            if (users == null || !users.Any())
            {
                return NotFound(new ValidationResult { Result = false, Message = "User not found" });
            }
            _context.Users.RemoveRange(users);



            var address = _context.Addresses.Where(a => a.UserId == userId);
            _context.Addresses.RemoveRange(address);

            var orders = _context.Orders.Where(a => a.UserId == userId);
            _context.Orders.RemoveRange(orders);

            

            var result = await _context.SaveChangesAsync();
            if (result >= 0)
                return Ok(new ValidationResult { Result = true, Message = $"User deleted successfully." });

            else
                return BadRequest(new ValidationResult { Result = false, Message = "Fehler beim Löschen des Benutzers. Bitte versuchen Sie es später erneut." });
        }
    }
}
