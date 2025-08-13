using Microsoft.EntityFrameworkCore;

namespace TechNova.Models
{
    // EF Core database context for TechNova: central place to expose entities and configure mappings.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Entity sets (tables) available to EF Core.
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }


        // Model configuration: enforce decimal precision for monetary/ratings/discount fields.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price)
                      .HasPrecision(10, 2);

                entity.Property(p => p.AvgRating)
                      .HasPrecision(2, 1);

                // Add precision for discount fields:
                entity.Property(p => p.DiscountPercent)
                      .HasPrecision(5, 2);

                entity.Property(p => p.DiscountedPrice)
                      .HasPrecision(10, 2);
            });
        }

    }
}
