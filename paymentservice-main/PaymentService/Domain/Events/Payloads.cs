namespace PaymentService.Domain.Events;

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
    Guid OrderId,
    DateTime AcceptedAt
);

public record RestaurantRejectedPayload(
    Guid OrderId,
    string Reason
);

public record RestaurantOrderReadyPayload(
    Guid OrderId,
    DateTime ReadyAt
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
    Guid OrderId,
    string Reason
);

public record DeliveryAssignedPayload(
    Guid OrderId,
    DateTime AssignedAt
);

public record DeliveryStartedPayload(
    Guid OrderId,
    DateTime StartedAt
);

public record DeliveryCompletedPayload(
    Guid OrderId,
    DateTime CompletedAt
);

public record CancelledPayload(
    Guid OrderId,
    string Reason
);