using Microsoft.AspNetCore.Identity;

namespace WAHShopBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public bool IsGuest { get; set; }
        public bool IsAktiv { get; set; }
        public string SignupProvider { get; set; } = string.Empty;
    }
}
