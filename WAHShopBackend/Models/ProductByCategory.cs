namespace WAHShopBackend.Models
{
    public class ProductByCategory
    {
        public Categories Category { get; set; } = new Categories();
        public List<Product> Products { get; set; } = new List<Product>();
    }
}
