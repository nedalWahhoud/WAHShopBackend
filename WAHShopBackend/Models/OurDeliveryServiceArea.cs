namespace WAHShopBackend.Models
{
    public class OurDeliveryServiceArea
    {
        public int Id { get; set; }
        public string RegionName { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public double MinimumPurchaseAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
