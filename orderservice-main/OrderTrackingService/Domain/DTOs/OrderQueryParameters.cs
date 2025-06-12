namespace OrderTrackingService.Domain.DTOs;

public class OrderQueryParameters
{
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? RestaurantStatus { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
