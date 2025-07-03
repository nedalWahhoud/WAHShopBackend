using System.ComponentModel.DataAnnotations;

namespace WAHShopBackend.Models
{
    public class ForgotPassword
    {
        public string? Email { get; set; }
        public string EmailPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string PasswordAgain { get; set; } = string.Empty;
    }
}
