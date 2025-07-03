using Microsoft.AspNetCore.Mvc;
using WAHShopBackend.EmailF;
using WAHShopBackend.Models;
using WAHShopBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailsController(MyDbContext context, EmailService emailService, IOptions<JwtSettings> jwtSettings) : ControllerBase
    {
        private readonly EmailService _emailService = emailService;
        private readonly MyDbContext _context = context;
        private readonly IOptions<JwtSettings> _jwtSettings = jwtSettings;

        [HttpPost("sendMail")]
        public async Task<IActionResult> SendMail([FromBody] EmailRequest emailRequest)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailRequest.ToEmail.ToLower());
                if (user == null)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "User not found." });
                }

                // add Random Password to database

                string randomPassword = GenerateRandomPassword();
                user.Password = BCrypt.Net.BCrypt.HashPassword(randomPassword.Trim());
                PasswordReset passwordReset = new PasswordReset
                {
                    UserId = user.Id,
                    RandomPassword = BCrypt.Net.BCrypt.HashPassword(randomPassword.Trim()),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10) // Set expiration time to 10 minutes from now
                };
                _context.PasswordReset.Add(passwordReset);
                var result = await _context.SaveChangesAsync();
                if (result <= 0)
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Failed to save password reset information." });
                }
                // add body to email request
                emailRequest.Body = "Hello " + user.UserName + ",<br><br>" +
                                   "Ihr neues Passwort ist: <strong>" + randomPassword + "</strong><br><br>" +
                                   "Bitte ändern Sie das Passwort, das gesendete Passwort läuft in 10 Minuten ab.<br><br>" +
                                   "Viele Grüße,<br>Syriana Team";

                await Task.Delay(1000); // Simulating async work
                await _emailService.SendEmailAsync(emailRequest.ToEmail, emailRequest.Subject, emailRequest.Body);
                return Ok(new ValidationResult { Result = true, Message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        public static string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ForgotPassword forgotPassword)
        {
            if (forgotPassword == null || string.IsNullOrWhiteSpace(forgotPassword.Email) || string.IsNullOrWhiteSpace(forgotPassword.NewPassword))
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid request data." });
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == forgotPassword.Email.ToLower());
                if (user == null)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "User not found." });
                }
                // Check if the password reset request exists and is valid
                var passwordReset = await _context.PasswordReset
                    .Where(pr => pr.UserId == user.Id && pr.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(pr => pr.Id)
                    .FirstOrDefaultAsync();

                if (passwordReset == null)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Invalid or expired password reset request." });
                }

                // Verify the temporary password
                bool isTempPasswordValid = BCrypt.Net.BCrypt.Verify(forgotPassword.EmailPassword.Trim(), passwordReset.RandomPassword);

                if (!isTempPasswordValid)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Temporary password is incorrect." });
                }

                // Update user's password
                user.Password = BCrypt.Net.BCrypt.HashPassword(forgotPassword.NewPassword.Trim());
                _context.Users.Update(user);

                // Remove the password reset entry
                _context.PasswordReset.Remove(passwordReset);

                var result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult
                {
                    Result = result > 0,
                    Message = result > 0 ? "Password reset successfully." : "Password reset failed."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
