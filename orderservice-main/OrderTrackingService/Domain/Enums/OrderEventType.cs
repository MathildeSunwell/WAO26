namespace OrderTrackingService.Domain.Enums;

public enum OrderEventType
{
    OrderCreated,
    RestaurantAccepted,
    RestaurantRejected,
    RestaurantOrderReady,
    RestaurantCancelled,
    PaymentReserved,
    PaymentFailed,
    PaymentSucceeded,
    PaymentCancelled,
    DeliveryAssigned,
    DeliveryStarted,
    DeliveryCompleted,
    DeliveryCancelled
}