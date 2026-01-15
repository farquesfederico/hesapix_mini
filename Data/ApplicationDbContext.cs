using Hesapix.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Stok> Stocks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Subscription)
                .WithOne(s => s.User)
                .HasForeignKey<Subscription>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Sales)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Payments)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Stocks)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sale relationships
            modelBuilder.Entity<Sale>()
                .HasMany(s => s.SaleItems)
                .WithOne(si => si.Sale)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Sale>()
                .HasMany(s => s.Payments)
                .WithOne(p => p.Sale)
                .HasForeignKey(p => p.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Stock relationships
            modelBuilder.Entity<Stok>()
                .HasMany(s => s.SaleItems)
                .WithOne(si => si.Stock)
                .HasForeignKey(si => si.StokId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Subscription>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            modelBuilder.Entity<Stok>()
                .HasIndex(s => new { s.UserId, s.ProductCode });

            modelBuilder.Entity<Sale>()
                .HasIndex(s => new { s.UserId, s.SaleDate });

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.UserId, p.PaymentDate });
        }
    }
}