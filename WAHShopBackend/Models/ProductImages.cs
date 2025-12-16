using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WAHShopBackend.Models
{
    public class ProductImages
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public int ProductId { get; set; }
        public DateTime LastModified { get; set; } 
        [NotMapped]
        public byte[]? ImageBytes { get; set; }
        [JsonIgnore]
        public Product? Product { get; set; }
    }
}
