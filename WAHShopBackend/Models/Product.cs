using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WAHShopBackend.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name_de { get; set; }
        public string? Description_de { get; set; }
        public int CategoryId { get; set; }
        public Categories? Category { get; set; }
        public string? Barcode { get; set; } = "BarcodeNull";
        public int Quantity { get; set; }
        public double PurchasePrice { get; set; }
        public double SalePrice { get; set; }
        public int MinimumStock { get; set; }
        public DateTime EXPDate { get; set; }
        public int SupplierId { get; set; }
        public Suppliers? Supplier { get; set; }
        public int UserId { get; set; }
        public ICollection<ProductImages> ProductImages { get; set; } = [];
        public string? Name_ar { get; set; }
        public string? Description_ar { get; set; }
        public int TaxRateId { get; set; }
        public TaxRate? TaxRate { get; set; }
        public int? ProductGroupID { get; set; }
        public GroupProducts? ProductGroup { get; set; }
        public bool IsShippable { get; set; } 
        public ProductDiscounts? ProductDiscount { get; set; }
    }
}
