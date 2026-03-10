using System.Text.Json.Serialization;

namespace WAHShopBackend.Models
{
    public class ProductDiscounts
    {
        public int Id { get; set; }
        public int ProductsId { get; set; }
        public double DiscountedPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        // Navigation property to the Product
        [JsonIgnore]
        public Product? Product { get; set; }
    }
}
