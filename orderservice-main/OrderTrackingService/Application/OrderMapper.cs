using System.Diagnostics;
using OrderTrackingService.Domain.DTOs;
using OrderTrackingService.Domain.Enums;
using OrderTrackingService.Infrastructure.Database.Models;

namespace OrderTrackingService.Application;

public static class OrderMapper
{
    public static Order ToEntity(this OrderDto? dto, Guid correlationId)
    {
        Debug.Assert(dto != null, nameof(dto) + " != null");
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CreateTime = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CorrelationId = correlationId,
            OrderStatus = OrderStatus.Pending.ToString(),
            DeliveryStatus = DeliveryStatus.Pending.ToString(),
            PaymentStatus = PaymentStatus.Pending.ToString(),
            RestaurantStatus = RestaurantStatus.Pending.ToString(),
            Comment = string.Empty
        };

        return order;
    }
}