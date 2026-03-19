namespace WAHShopBackend.Models
{
    public class UserPermission
    {
        public int UserId { get; set; }
        public int PermissionId { get; set; }

        public required User User { get; set; }
        public required Permission Permission { get; set; }
    }
}
