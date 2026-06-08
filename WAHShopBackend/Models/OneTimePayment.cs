using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WAHShopBackend.Models
{
    public class OneTimePayment
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int DistributionLineId { get; set; }
        public double TotalAmount { get; set; }
        public double AmountCollected { get; set; }
        public OneTimePaymentStatus Status { get; set; } 
        public string? Notes { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime? CreatedAt { get; set; }
        public DateTime PickupDate { get; set; }

        public Customers? Customer { get; set; } 
        public virtual DistributionLines? DistributionLine { get; set; }
    }
    public class UpdateStatusDto
    {
        public OneTimePaymentStatus Status { get; set; }

        public double AmountCollected { get; set; } = 0;
    }
    public class OneTimePaymentsGroupDto
    {
        public DateTime GroupPickupDate { get; set; } // Datum des ersten Tages in der Gruppe
        public List<OneTimePayment> Payments { get; set; } = [];
    }
    public enum OneTimePaymentStatus
    {
        [Display(Name = "Offen")]
        Offen = 0,
        [Display(Name = "Voll-inkassiert")]
        VollstaendigInkassiert = 1,

        [Display(Name = "Teil-inkassiert")]
        TeilweiseInkassiert = 2,
        [Display(Name = "Überzahlt")]
        Ueberzahlt = 3,
        [Display(Name = "Verschoben")]
        Verschoben = 4
    }
}
