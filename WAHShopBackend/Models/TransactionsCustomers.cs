using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WAHShopBackend.Models
{
    public class TransactionsCustomers
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime TransactionDate { get; set; }
        // Navigation property
        public Customers? Customer { get; set; }
    }
    public enum TransactionType
    {
        Borrow,
        Repay
    }
}
