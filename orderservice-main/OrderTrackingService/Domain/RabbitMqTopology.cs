namespace OrderTrackingService.Domain;

public static class RabbitMqTopology
{
    public const string EventExchange = "events.topic";
    public const string DefaultExchange = "";
    
    public const string EventQueue = "order-queue";
    public const string RetryQueue = "order-retry-queue";
    public const string DeadLetterQueue = "order-dlq-queue";

    public const string PublishOrderCreated = "order.created";
    
    public static readonly string[] RoutingKeys =
    [
        "payment.reserved",
        "payment.failed",
        "payment.succeeded",
        "payment.cancelled",
        "restaurant.rejected",
        "restaurant.accepted",
        "restaurant.order_ready",
        "restaurant.cancelled",
        "delivery.assigned",
        "delivery.started",
        "delivery.completed",
        "delivery.cancelled"
    ];
}