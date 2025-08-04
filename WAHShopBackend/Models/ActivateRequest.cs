namespace WAHShopBackend.Models
{
    public class ActivateRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
