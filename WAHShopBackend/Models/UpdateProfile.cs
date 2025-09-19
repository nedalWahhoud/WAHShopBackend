using System.ComponentModel.DataAnnotations;

namespace WAHShopBackend.Models
{
    public class UpdateProfile
    {
        public int UserId { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string PasswordAgain { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public UpdateTypeEnum UpdateType { get; set; }
    }
    public enum UpdateTypeEnum : byte
    {
        Password,
        Birthday
    }
}
