namespace OrderTrackingService.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Reserved,
    Failed,
    Succeeded,
    Cancelled
}