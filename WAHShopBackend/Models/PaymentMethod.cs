namespace WAHShopBackend.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string? Method { get; set; }
        public string? Description_de { get; set; }
        public string? Description_ar { get; set; }
        public int? BankTransferDetailsId { get; set; }
        public BankTransferDetails? BankTransferDetails { get; set; }
        public double? MinimumPurchaseAmount { get; set; }
    }
}
