using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Database.Models;

namespace PaymentService.Infrastructure.Database;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    // DBSET: Represents the "Payments" table in the database
    // DbSet<Payment> allows you to query and save Payment entities
    // null! = "I know this will be set by EF, don't warn me about nullability"

    public DbSet<Payment> Payments { get; set; } = null!;

    // MODEL CONFIGURATION: This method defines how objects map to database tables
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(b =>
        {
            // PRIMARY KEY: Define which property is the primary key
            b.HasKey(p => p.Id);  

            // REQUIRED FIELD: OrderId cannot be null in the database
            b.Property(p => p.OrderId).IsRequired();

            // DECIMAL PRECISION: Configure money storage for SQL Server
            b.Property(p => p.Amount)
                .IsRequired()                       
                .HasColumnType("decimal(18,2)");    

            b.Property(p => p.Currency)
                .IsRequired()                        // Cannot be null
                .HasMaxLength(3);                    // Exactly 3 characters

            // ENUM CONVERSION: Store enum as string instead of integer
            b.Property(p => p.Status)
                .IsRequired()                        
                .HasConversion(new EnumToStringConverter<PaymentStatus>());

            // DEFAULT VALUES: SQL Server automatically sets these when inserting
            // GETDATE() is SQL Server function for current date/time
            // Note: These run on the database server, not in the code
            b.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");   // Auto-set when record created

            b.Property(p => p.LastUpdated)
                .HasDefaultValueSql("GETDATE()");   // Auto-set when record created

            // Note: LastUpdated should ideally auto-update on changes, but that requires triggers
            // or manual updates in your repository code
        });
    }
}
