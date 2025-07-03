namespace WAHShopBackend.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string? Method { get; set; }
        public string? Description_de { get; set; }
        public string? Description_ar { get; set; }
    }
}
