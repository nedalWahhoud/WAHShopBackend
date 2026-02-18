using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Models;

namespace WAHShopBackend.Data
{
    public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImages> ProductImages { get; set; }
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
        public DbSet<BankTransferDetails> BankTransferDetails { get; set; }
        public DbSet<ShippingProvider> ShippingProviders { get; set; }
        public DbSet<OurDeliveryServiceArea> OurDeliveryServiceArea { get; set; }
        public DbSet<CarouselImage> CarouselImage { get; set; }
        public DbSet<DistributionLines> DistributionLines { get; set; }
        public DbSet <Customers> Customers { get; set; }
        public DbSet<DebtCustomers> DebtCustomers { get; set; }
        public DbSet<TransactionsCustomers> TransactionsCustomers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // cascade Product delete ProductImages
            modelBuilder.Entity<Product>()
               .HasMany(p => p.ProductImages)
               .WithOne(pi => pi.Product)
               .HasForeignKey(pi => pi.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
            // cascade Order delete OrderItems
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            // trigger TransactionsCustomers create DebtCustomers
            modelBuilder.Entity<TransactionsCustomers>()
            .ToTable(tb => tb.HasTrigger("trg_UpdateDebt"));
            // Trigger pin generation
            modelBuilder.Entity<Customers>()
            .ToTable(tb => tb.HasTrigger("trg_Customers_GeneratePIN"));
            // enum to string TransactionsCustomers.Type
            modelBuilder.Entity<TransactionsCustomers>()
            .Property(e => e.Type)
            .HasConversion<string>();

            // cascade Customers delete TransactionsCustomers
            modelBuilder.Entity<Customers>()
            .HasMany(t => t.Transactions)
            .WithOne(c => c.Customer)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
            // cascade Customers delete DebtCustomers
            modelBuilder.Entity<Customers>()
           .HasMany(t => t.DebtCustomers)
           .WithOne(c => c.Customer)
           .HasForeignKey(t => t.CustomerId)
           .OnDelete(DeleteBehavior.Cascade);
            // trigger DebtCustomers delete TransactionsCustomers
            modelBuilder.Entity<DebtCustomers>()
            .ToTable(tb => tb.HasTrigger("trg_DeleteTransactionsOnDebtDelete"));
        }
    }
}
