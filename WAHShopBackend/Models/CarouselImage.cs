using System.ComponentModel.DataAnnotations.Schema;

namespace WAHShopBackend.Models
{
    public class CarouselImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime LastModified { get; set; }
        [NotMapped]
        public byte[]? ImageBytes { get; set; }
    }
}
