using PaymentService.Domain.Events;

namespace PaymentService.Application.Services;

public interface IPaymentService
{
    // Handles an OrderCreatedEvent from RabbitMQ and creates payment
    Task ProcessOrderCreatedAsync(OrderCreatedPayload orderEvent, Guid correlationId);

    // Handles a RestaurantRejectedEvent from RabbitMQ and cancels payment
    Task ProcessRestaurantRejectedAsync(RestaurantRejectedPayload restaurantRejectedEvent, Guid correlationId);
    
    // Updates an existing payment to Succeeded status based on DeliveryStartedEvent
    // This marks the payment as completed after the delivery starts
    Task FinalizePaymentAsync(DeliveryStartedPayload deliveryEvent, Guid correlationId);
}