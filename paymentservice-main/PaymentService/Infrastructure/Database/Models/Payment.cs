using PaymentService.Domain.Enums;

namespace PaymentService.Infrastructure.Database.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}