namespace WAHShopBackend.Models
{
    public class CustomerLoginDto
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string PIN { get; set; } = "";
    }
}
