namespace WAHShopBackend.Models
{
    public class ValidationResult
    {
        public bool Result { get; set; } 
        public string? Message { get; set; }
        public int? NewId { get; set; }
    }
}
