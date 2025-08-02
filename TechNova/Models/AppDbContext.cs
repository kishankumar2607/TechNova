using Microsoft.EntityFrameworkCore;

namespace TechNova.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price)
                      .HasPrecision(10, 2); // e.g. 99999999.99

                entity.Property(p => p.AvgRating)
                      .HasPrecision(2, 1); // e.g. 4.5

                // Add precision for discount fields:
                entity.Property(p => p.DiscountPercent)
                      .HasPrecision(5, 2); // e.g. 99.99%

                entity.Property(p => p.DiscountedPrice)
                      .HasPrecision(10, 2); // e.g. 1999.99
            });
        }

    }
}
