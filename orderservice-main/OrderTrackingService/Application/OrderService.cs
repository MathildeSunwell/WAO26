using OrderTrackingService.Domain.DTOs;
using OrderTrackingService.Domain.Enums;
using OrderTrackingService.Domain.Events;
using OrderTrackingService.Infrastructure.Database;
using OrderTrackingService.Infrastructure.Messaging;

namespace OrderTrackingService.Application;

public class OrderService(
    IOrderRepository orderRepo,
    IOrderEventPublisher orderEventPublisher,
    ILogger<OrderService> logger) 
    : IOrderService
{
    public async Task<Guid> ProcessCreateOrderAsync(Guid correlationId, OrderDto? orderDto)
    {
        var order = orderDto.ToEntity(correlationId);
        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Order saved");
        
        var items = orderDto!.OrderItems
            .Select(i => new ItemDto(
                ItemId: Guid.NewGuid(),
                ProductName: i.ProductName,
                Quantity: i.Quantity,
                Price: i.Price))
            .ToList();

        var payload = new OrderCreatedPayload(
            OrderId: order.OrderId,
            CustomerAddress: orderDto.CustomerAddress,
            Items: items,
            TotalPrice: orderDto.TotalPrice,
            Currency: "DKK");
        
        var eventEnvelope = new EventEnvelope<OrderCreatedPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.OrderCreated,
            Timestamp = DateTime.UtcNow,
            Payload = payload
        };
    
        await orderEventPublisher.PublishOrderCreatedAsync(eventEnvelope, correlationId);
        logger.LogInformation("Order event published");
        return order.OrderId;
    }

    public async Task ProcessRestaurantAcceptedAsync(EventEnvelope<RestaurantAcceptedPayload> eventEnvelope, 
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.RestaurantStatus = RestaurantStatus.Accepted.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Order accepted with ID: {orderId}", orderId);
    }

    public async Task ProcessRestaurantRejectedAsync(EventEnvelope<RestaurantRejectedPayload> eventEnvelope, 
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }
        order.Comment = eventEnvelope.Payload.Reason;
        order.RestaurantStatus = RestaurantStatus.Rejected.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Order rejected with ID: {orderId}", orderId);
    }

    public async Task ProcessRestaurantOrderReadyAsync(EventEnvelope<RestaurantOrderReadyPayload> eventEnvelope,
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.RestaurantStatus = RestaurantStatus.Ready.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Order marked as ready with ID: {orderId}", orderId);
    }

    public async Task ProcessRestaurantCancelledAsync(EventEnvelope<RestaurantCancelledPayload> eventEnvelope, Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }
        order.RestaurantStatus = RestaurantStatus.Cancelled.ToString();
        order.OrderStatus = OrderStatus.Cancelled.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Restaurant cancelled order with ID: {orderId}", orderId);
    }

    public async Task ProcessPaymentReservedAsync(EventEnvelope<PaymentReservedPayload> eventEnvelope,
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.PaymentStatus = PaymentStatus.Reserved.ToString();
        order.OrderStatus = OrderStatus.Processing.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Payment reserved for order with ID: {orderId}", orderId);
    }

    public async Task ProcessPaymentFailedAsync(EventEnvelope<PaymentFailedPayload> eventEnvelope, Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }
        order.Comment = eventEnvelope.Payload.Reason;
        order.PaymentStatus = PaymentStatus.Failed.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Payment failed for order with ID: {orderId}", orderId);
    }

    public async Task ProcessPaymentSucceededAsync(EventEnvelope<PaymentSucceededPayload> eventEnvelope,
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.PaymentStatus = PaymentStatus.Succeeded.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Payment succeeded for order with ID: {orderId}", orderId);
    }

    public async Task ProcessPaymentCancelledAsync(EventEnvelope<PaymentCancelledPayload> eventEnvelope, Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }
        order.PaymentStatus = PaymentStatus.Cancelled.ToString();
        order.OrderStatus = OrderStatus.Cancelled.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Payment cancelled for order with ID: {orderId}", orderId);
    }

    public async Task ProcessDeliveryAssignedAsync(EventEnvelope<DeliveryAssignedPayload> eventEnvelope, 
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.DeliveryStatus = DeliveryStatus.Assigned.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Delivery assigned for order with ID: {orderId}", orderId);
    }

    public async Task ProcessDeliveryStartedAsync(EventEnvelope<DeliveryStartedPayload> eventEnvelope,
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.DeliveryStatus = DeliveryStatus.Started.ToString();
        order.RestaurantStatus = RestaurantStatus.Completed.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Delivery started for order with ID: {orderId}", orderId);
    }

    public async Task ProcessDeliveryCompletedAsync(EventEnvelope<DeliveryCompletedPayload> eventEnvelope,
        Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.DeliveryStatus = DeliveryStatus.Completed.ToString();
        order.OrderStatus = OrderStatus.Completed.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Delivery completed for order with ID: {orderId}", orderId);
    }

    public async Task ProcessDeliveryCancelledAsync(EventEnvelope<DeliveryCancelledPayload> eventEnvelope, Guid correlationId)
    {
        var orderId = eventEnvelope.Payload.OrderId;
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order not found with ID: {orderId}", orderId);
            return;
        }

        order.DeliveryStatus = DeliveryStatus.Cancelled.ToString();
        order.OrderStatus = OrderStatus.Cancelled.ToString();
        await orderRepo.UpdateAsync(order);
        await orderRepo.SaveChangesWithChangeCheckAsync();
        logger.LogInformation("Delivery cancelled for order with ID: {orderId}", orderId);
    }

    public async Task<OrderResponseDto?> GetOrderAsync(Guid orderId)
    {
        var order = await orderRepo.GetByOrderIdAsync(orderId);
        if (order == null) return null;

        return new OrderResponseDto
        {
            OrderId = order.OrderId,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            RestaurantStatus = order.RestaurantStatus,
            DeliveryStatus = order.DeliveryStatus,
            CreateTime = order.CreateTime,
            Comment = order.Comment
        };
    }
    
    public async Task<IEnumerable<OrderResponseDto>> GetOrdersAsync(OrderQueryParameters q)
    {
        var orders = await orderRepo.GetByFilterAsync(q);
        return orders.Select(o => new OrderResponseDto {
            OrderId         = o.OrderId,
            OrderStatus     = o.OrderStatus,
            PaymentStatus   = o.PaymentStatus,
            RestaurantStatus = o.RestaurantStatus,
            DeliveryStatus  = o.DeliveryStatus,
            CreateTime      = o.CreateTime,
            Comment         = o.Comment
        });
    }
}

public interface IOrderService
{
    Task<Guid> ProcessCreateOrderAsync(Guid correlationId, OrderDto? orderDto);
    Task ProcessRestaurantAcceptedAsync(EventEnvelope<RestaurantAcceptedPayload> eventEnvelope, Guid correlationId);
    Task ProcessRestaurantRejectedAsync(EventEnvelope<RestaurantRejectedPayload> eventEnvelope, Guid correlationId);
    Task ProcessRestaurantOrderReadyAsync(EventEnvelope<RestaurantOrderReadyPayload> eventEnvelope, Guid correlationId);
    Task ProcessRestaurantCancelledAsync(EventEnvelope<RestaurantCancelledPayload> eventEnvelope, Guid correlationId);
    Task ProcessPaymentReservedAsync(EventEnvelope<PaymentReservedPayload> eventEnvelope, Guid correlationId);
    Task ProcessPaymentFailedAsync(EventEnvelope<PaymentFailedPayload> eventEnvelope, Guid correlationId);
    Task ProcessPaymentSucceededAsync(EventEnvelope<PaymentSucceededPayload> eventEnvelope, Guid correlationId);
    Task ProcessPaymentCancelledAsync(EventEnvelope<PaymentCancelledPayload> eventEnvelope, Guid correlationId);
    Task ProcessDeliveryAssignedAsync(EventEnvelope<DeliveryAssignedPayload> eventEnvelope, Guid correlationId);
    Task ProcessDeliveryStartedAsync(EventEnvelope<DeliveryStartedPayload> eventEnvelope, Guid correlationId);
    Task ProcessDeliveryCompletedAsync(EventEnvelope<DeliveryCompletedPayload> eventEnvelope, Guid correlationId);
    Task ProcessDeliveryCancelledAsync(EventEnvelope<DeliveryCancelledPayload> envelope, Guid correlationId);
    Task<OrderResponseDto?> GetOrderAsync(Guid orderId);
    Task<IEnumerable<OrderResponseDto>> GetOrdersAsync(OrderQueryParameters q);
}

