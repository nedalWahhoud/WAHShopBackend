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
using System.Linq.Expressions;


namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(MyDbContext context, IOptions<JwtSettings> jwtSettings, EmailService emailService, UserManager<UserIdentity> userManager,SignInManager<UserIdentity> signInManager,AppConfig appConfig) : ControllerBase
    {
        private readonly IOptions<JwtSettings> _jwtSettings = jwtSettings;
        private readonly MyDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        private UserManager<UserIdentity> _userManager = userManager;
        private readonly SignInManager<UserIdentity> _signInManager = signInManager;
        private readonly AppConfig _appConfig = appConfig;
        private const string Permission = "Permission";
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            try
            {
                if (loginModel.SignupProvider == "Manual")
                {
                    // Es konnte dass bei login die UserName oder Email verwendet werden kann
                    var user = await _context.Users
                               .Include(u => u.UserPermissions)
                               .ThenInclude(up => up.Permission)
                               .FirstOrDefaultAsync(u =>
                               u.Email!.ToLower() == loginModel.Email!.ToLower()
                               && u.IsGuest == false && u.SignupProvider == "Manual" && u.IsAktiv == true);

                    if (user == null || !BCrypt.Net.BCrypt.Verify(loginModel!.Password.Trim(), user!.Password!.Trim()) || user.IsGuest == true)
                        return Unauthorized(new ValidationResult { Result = false, Message = "Falsches Passwort oder E-Mail." });
                    if (user.IsAktiv == false)
                        return Unauthorized(new ValidationResult { Result = false, Message = "Benutzer ist nicht aktiv" });

                    string token = GetToken(user,user?.UserPermissions);

                    return Ok(new LoginModel { Token = token });
                }
                else
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Ungültiger Anmeldeanbieter" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error " + ex.Message });
            }
        }
        private string GetToken(User user, List<UserPermission>? userPermissions = null)
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

            if(userPermissions != null)
            {
                foreach (var userPermission in userPermissions)
                {
                    claims = [.. claims, new Claim(Permission, userPermission.Permission.Name)];
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Key!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                claims: claims,
                expires: DateTime.Now.AddDays(365),
                signingCredentials: creds
            );
            
            return new JwtSecurityTokenHandler().WriteToken(jwtToken);

        }
        // get: api/users
        [HttpGet("getUserById/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return Unauthorized(new ValidationResult { Result = false, Message = "Benutzer nicht gefunden"} );

            if (user.IsAktiv == false)
            {
                return Unauthorized(new ValidationResult { Result = false, Message = "Benutzer ist nicht aktiv" });
            }

            string token = GetToken(user, user?.UserPermissions);

            return Ok(new LoginModel { Token = token });

        }
        [HttpGet("getAllUsers")]
        public async Task<IActionResult> GetAllUsers([FromQuery] GetItems<User> getItems)
        {
            if (getItems.AllItemsLoaded == true) return Ok(new GetItems<User>() { Items = [], AllItemsLoaded = true });

            try
            {
                List<User> users = await _context.Users
                    .OrderByDescending(x => x.Id)
                    .Skip(getItems.CurrentPage * getItems.PageSize)
                    .Take(getItems.PageSize)
                    .ToListAsync();

                if(users.Count == 0)
                {
                    getItems.AllItemsLoaded = true;
                }
               
                getItems.Items = users;
                getItems.CurrentPage++;
                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPost("signup")]
        public async Task<ActionResult<User>> Signup(SignupModel signupModel)
        {
            try
            {
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
                        UserIdentity userIdentity = new()
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
                    UserIdentity userIdentity = new()
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

                    //  Stellen Sie sicher, dass das neue Passwort sich vom alten unterscheidet.
                    bool isSameAsOld = BCrypt.Net.BCrypt.Verify(updateProfile.NewPassword, existingUser.Password);
                    if (isSameAsOld)
                    {
                        return BadRequest(new ValidationResult { Result = false, Message = "Das neue Passwort darf nicht gleich dem alten sein." });
                    }
                    //  Aktualisieren Sie Ihr Passwort nach der Verschlüsselung.
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
                    return Ok(new LoginModel() { Token = GetToken(existingUser) });
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
            // if user email exist
            var checkEmail = await _context.Users.AnyAsync(u => u.Email.ToLower() == userGuest.Email.ToLower() && u.IsGuest == false);
            if (checkEmail)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "EmailExists" });
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
        //  Redirect to Google
        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] bool? rememberMe)
        {
            var redirectUrl = Url.Action("GoogleResponse", "Users");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);

            properties.Items["rememberMe"] = rememberMe.ToString();


            return Challenge(properties, "Google");
        }
        //  Callback from Google
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return BadRequest("Error loading external login info.");

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var userName = info.Principal.FindFirstValue(ClaimTypes.Name);
            // get rememberMe value
            bool rememberMe = false;
            if (info.AuthenticationProperties?.Items != null &&
                info.AuthenticationProperties.Items.TryGetValue("rememberMe", out var rememberMeValue))
            {
                _ = bool.TryParse(rememberMeValue, out rememberMe);
            }

            if (email == null || userName == null)
            {
                return BadRequest("Email or userName not found from Google.");
            }

            Expression<Func<User, bool>> userFilter = u =>
                 u.Email.ToLower() == email.ToLower()
                 && !u.IsGuest
                 && u.IsAktiv;

            var userExists = await _context.Users.AnyAsync(userFilter);
            string jwtToken = null!;
            if (userExists)
            {
                // User exist, proceed to sign in
                var user = await _context.Users.FirstOrDefaultAsync(userFilter);
                if (user == null)
                    return BadRequest("Benutzer nicht gefunden.");

                jwtToken = GetToken(user);
            }
            else
            {
                // wenn der User nicht existiert, dann signup ohne Passwort und bestätigungslink da es ein Google Login ist
                User newUser = new()
                {
                    UserName = userName,
                    Password = "",
                    Email = email,
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
                    jwtToken = GetToken(newUser);
                }
                else
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Fehler beim Erstellen des Benutzers" });
                }
            }
            return Redirect($"{_appConfig.Domin}/auth-success?token={jwtToken}&rememberMe={rememberMe}");
        }
    }
}
