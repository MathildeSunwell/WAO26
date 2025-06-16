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


/*
    This file defines all the event payload record types used for messaging between services.

    Each record represents the structure of a specific domain event – for example:
    - OrderCreatedPayload: sent when a customer creates an order
    - PaymentFailedPayload: sent when a payment attempt fails
    - DeliveryCompletedPayload: sent when the delivery process finishes

    These payloads are:
    - Included as the 'Payload' in EventEnvelope<TPayload> messages
    - Serialized to JSON when published to RabbitMQ
    - Deserialized back when consumed by other services

    Purpose:
    - Provide a clear and consistent contract for each message type
    - Ensure strong typing and validation within the system
    - Decouple services by defining shared event data in a central place

    This approach makes the event-driven architecture predictable, type-safe,
    and easy to evolve as the domain grows.
*/
