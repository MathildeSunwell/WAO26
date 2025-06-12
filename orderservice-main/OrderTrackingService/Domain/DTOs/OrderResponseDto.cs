namespace OrderTrackingService.Domain.DTOs;

public class OrderResponseDto
{
    public Guid OrderId { get; set; }
    public string OrderStatus { get; set; }
    public string PaymentStatus { get; set; }
    public string RestaurantStatus { get; set; }
    public string DeliveryStatus { get; set; }
    public DateTime CreateTime { get; set; }
    public string Comment { get; set; }
}

