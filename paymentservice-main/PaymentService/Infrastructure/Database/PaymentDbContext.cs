using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Database.Models;

namespace PaymentService.Infrastructure.Database;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(p => p.Id);

            b.Property(p => p.OrderId).IsRequired();

            // decimal-precision for SQL Server
            b.Property(p => p.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            b.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(3);

            // Configure the enum to be stored as a string
            b.Property(p => p.Status)
                .IsRequired()
                .HasConversion(new EnumToStringConverter<PaymentStatus>());

            b.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
                
            b.Property(p => p.LastUpdated)
                .HasDefaultValueSql("GETDATE()");
        });
    }
}