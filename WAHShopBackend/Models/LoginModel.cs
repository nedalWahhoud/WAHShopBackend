namespace WAHShopBackend.Models
{
    public class LoginModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string BirthDate { get; set; } = DateTime.Now.ToString("yyyy.MM.dd");
        public bool IsGuest { get; set; }
        public string SignupProvider { get; set; } = string.Empty;
    }
}
