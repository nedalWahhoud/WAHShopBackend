namespace WAHShopBackend.Models
{
    public class DiscountCategory
    {
        public int Id { get; set; }
        public int CategoriesId { get; set; }
        public Categories? Category { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DiscountPercentage { get; set; }
        public int UsageLimit { get; set; }
        public int TimesUsed { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
