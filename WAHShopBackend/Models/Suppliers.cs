namespace WAHShopBackend.Models
{
    public class Suppliers
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Street { get; set; }
        public string? HNumber { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
    }
}
