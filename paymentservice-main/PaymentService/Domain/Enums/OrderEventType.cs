namespace PaymentService.Domain.Enums;

public enum OrderEventType
{
    OrderCreated,
    RestaurantAccepted,
    RestaurantRejected,
    RestaurantOrderReady,
    PaymentReserved,
    PaymentFailed,
    PaymentSucceeded,
    PaymentCancelled,
    DeliveryAssigned,
    DeliveryStarted,
    DeliveryCompleted,
    Cancelled
}