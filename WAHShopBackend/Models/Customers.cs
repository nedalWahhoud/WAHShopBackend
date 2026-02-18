using System.Text.Json.Serialization;

namespace WAHShopBackend.Models
{
    public class Customers
    {
        public int Id { get; set; }
        public string Name_de { get; set; } = string.Empty;
        public string Name_ar { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string BuildingNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Notes_de { get; set; }
        public string? Notes_ar { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int StopNumber { get; set; }
        public string? PIN { get; set; }
        // 🔗 FK
        public int DistributionLineId { get; set; }
        public virtual DistributionLines? DistributionLine { get; set; }

        [JsonIgnore]
        // 🔗 Navigation property
        public ICollection<TransactionsCustomers>? Transactions { get; set; }
        [JsonIgnore]
        // 🔗 Navigation property
        public ICollection<DebtCustomers>? DebtCustomers { get; set; }

    }
}
