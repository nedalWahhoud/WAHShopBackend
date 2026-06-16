using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WAHShopBackend.Models
{
    public class UserFavorite
    {
        public int UserId { get; set; }

        public int ProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [NotMapped]
        [JsonIgnore]
        public User? User { get; set; }
        [JsonIgnore, NotMapped]
        public Product? Product { get; set; }
    
    }
}
