namespace OrderTrackingService.Domain.Events;

public record ItemDto(
    Guid ItemId,
    string ProductName,
    int Quantity,
    decimal Price
);

public record OrderCreatedPayload(
    Guid OrderId,
    string CustomerAddress,
    List<ItemDto> Items,
    decimal TotalPrice,
    string Currency
);

public record RestaurantAcceptedPayload(
    Guid OrderId
);

public record RestaurantRejectedPayload(
    Guid OrderId,
    string Reason
);

public record RestaurantOrderReadyPayload(
    Guid OrderId
);

public record RestaurantCancelledPayload(
    Guid OrderId
);

public record PaymentReservedPayload(
    Guid OrderId
);

public record PaymentFailedPayload(
    Guid OrderId,
    string Reason
);

public record PaymentSucceededPayload(
    Guid OrderId
);

public record PaymentCancelledPayload(
    Guid OrderId
);

public record DeliveryAssignedPayload(
    Guid OrderId
);

public record DeliveryStartedPayload(
    Guid OrderId
);

public record DeliveryCompletedPayload(
    Guid OrderId
);

public record DeliveryCancelledPayload(
    Guid OrderId
);