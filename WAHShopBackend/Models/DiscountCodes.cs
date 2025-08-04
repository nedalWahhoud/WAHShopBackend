using System.ComponentModel.DataAnnotations;

namespace WAHShopBackend.Models
{
    public class DiscountCodes
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DiscountPercentage { get; set; }
        public int UsageLimit { get; set; }
        public int TimesUsed { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
