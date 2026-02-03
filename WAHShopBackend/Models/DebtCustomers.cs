namespace WAHShopBackend.Models
{
    public class DebtCustomers
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Balance { get; set; }
        public  Customers? Customer { get; set; }
    }
}
