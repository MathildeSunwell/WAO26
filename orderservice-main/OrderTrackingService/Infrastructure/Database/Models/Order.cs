using System.ComponentModel.DataAnnotations;

namespace OrderTrackingService.Infrastructure.Database.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderStatus { get; set; }
    public string DeliveryStatus { get; set; }
    public string PaymentStatus { get; set; }
    public string RestaurantStatus { get; set; }
    public string Comment { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdated { get; set; }
    
    [Timestamp]
    public byte[] ChangeCheck { get; set; }
}