namespace WAHShopBackend.Models
{
    public class Categories
    {
        public int Id { get; set; }
        public string? Name_de { get; set; }
        public string? Name_ar { get; set; }
        public bool Requires18Plus { get; set; }
        public bool IsAktiv { get; set; }
    }
}
