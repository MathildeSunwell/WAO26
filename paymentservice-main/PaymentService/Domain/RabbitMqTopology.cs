using PaymentService.Domain.Enums;

namespace PaymentService.Domain;

public static class RabbitMqTopology
{
    // Main exchange for all services
    public const string EventExchange = "events.topic";
    public const string DefaultExchange = "";

    // Queue name for PaymentService
    public const string EventQueue = "payment-queue";
    public const string RetryQueue = "payment-retry-queue";
    public const string DeadLetterQueue = "payment-dlq-queue";
    
    // Routing keys for messages we publish
    public const string PaymentReservedKey = "payment.reserved";
    public const string PaymentFailedKey = "payment.failed";
    public const string PaymentSucceededKey = "payment.succeeded";
    public const string PaymentCancelledKey = "payment.cancelled";
    
    // Routing keys for messages we consume
    public const string OrderCreatedKey = "order.created";
    public const string RestaurantRejectedKey = "restaurant.rejected";
    public const string DeliveryStartedKey = "delivery.started";
    
    // All routing keys we consume (for queue binding)
    public static readonly string[] RoutingKeys = {
        OrderCreatedKey,
        RestaurantRejectedKey,
        DeliveryStartedKey
    };
    
    // Maps event types to routing keys for publishing
    public static string GetRoutingKeyForEventType(OrderEventType eventType) => eventType switch
    {
        OrderEventType.PaymentReserved => PaymentReservedKey,
        OrderEventType.PaymentFailed => PaymentFailedKey,
        OrderEventType.PaymentSucceeded => PaymentSucceededKey,
        OrderEventType.PaymentCancelled => PaymentCancelledKey,
        _ => throw new ArgumentOutOfRangeException(nameof(eventType), $"Unexpected event type: {eventType}")
    };
}