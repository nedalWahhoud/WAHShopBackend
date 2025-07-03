using System.ComponentModel.DataAnnotations;

namespace WAHShopBackend.Models
{
    public class SignupModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PasswordAgain { get; set; } = string.Empty;
        public string BirthDate { get; set; } = DateTime.Now.ToString("yyyy.MM.dd");
        public bool IsGuest { get; set; } = false;
    }
}
