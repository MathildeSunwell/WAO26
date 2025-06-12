using Microsoft.EntityFrameworkCore;
using OrderTrackingService.Infrastructure.Database.Models;

namespace OrderTrackingService.Infrastructure.Database;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.CreateTime)
                .HasDefaultValueSql("GETDATE()");
            b.Property(o => o.LastUpdated)
                .HasDefaultValueSql("GETDATE()");
        });
    }
}