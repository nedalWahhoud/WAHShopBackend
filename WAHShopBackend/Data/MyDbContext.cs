using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Models;
namespace WAHShopBackend.Data
{
    public class MyDbContext:DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Manufacturers> Manufacturers { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<PasswordReset> PasswordReset { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<OrderStatus> OrderStatus { get; set; }
        public DbSet<TaxRate> TaxRates { get; set; }
        public DbSet<GroupProducts> GroupProducts { get; set; }

    }
}
