namespace WAHShopBackend.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RandomPassword { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
