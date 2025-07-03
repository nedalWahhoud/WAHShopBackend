namespace WAHShopBackend.Models
{
    public class SearchProducts
    {
        public string? SearchTerm { get; set; }
        public List<Product> Products { get; set; } = [];
    }
}
