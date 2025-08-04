using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Models;

namespace WAHShopBackend.Data
{
    public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Manufacturers> Manufacturers { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderStatus> OrderStatus { get; set; }
        public DbSet<OrderItems> OrderItems { get; set; }
        public DbSet<PasswordReset> PasswordReset { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
       
        public DbSet<TaxRate> TaxRates { get; set; }
        public DbSet<GroupProducts> GroupProducts { get; set; }
        public DbSet<DiscountCodes> DiscountCodes { get; set; }
        public DbSet<DiscountCategory> DiscountCategory { get; set; }
        public DbSet<BankTransferDetails> BankTransferDetails{ get; set;}
        public DbSet<ShippingProvider> ShippingProviders { get; set; }
    }
}
