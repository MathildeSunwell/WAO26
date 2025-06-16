using PaymentService.Domain.Enums;
using PaymentService.Domain.Events;
using PaymentService.Infrastructure.Database.Models;

namespace PaymentService.Domain.Mappers;

public static class PaymentEventMapper
{
    // Maps a Payment to a PaymentReservedPayload
    public static PaymentReservedPayload ToReservedPayload(this Payment payment)
    {
        return new PaymentReservedPayload(
            OrderId: payment.OrderId
        );
    }

    // Maps a Payment to a PaymentFailedPayload
    public static PaymentFailedPayload ToFailedPayload(this Payment payment, string reason = "Payment processing failed")
    {
        return new PaymentFailedPayload(
            OrderId: payment.OrderId,
            Reason: reason
        );
    }

    // Maps a Payment to a PaymentSucceededPayload
    public static PaymentSucceededPayload ToSucceededPayload(this Payment payment)
    {
        return new PaymentSucceededPayload(
            OrderId: payment.OrderId
        );
    }

    // Maps a Payment to a PaymentCancelledPayload
    public static PaymentCancelledPayload ToCancelledPayload(this Payment payment, string reason = "Payment cancelled")
    {
        return new PaymentCancelledPayload(
            OrderId: payment.OrderId,
            Reason: reason
        );
    }

    // Creates an envelope for a PaymentReservedPayload
    public static EventEnvelope<PaymentReservedPayload> ToReservedEventEnvelope(this Payment payment)
    {
        return new EventEnvelope<PaymentReservedPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentReserved,
            Timestamp = DateTime.UtcNow,
            Payload = payment.ToReservedPayload()
        };
    }

    // Creates an envelope for a PaymentFailedPayload
    public static EventEnvelope<PaymentFailedPayload> ToFailedEventEnvelope(this Payment payment)
    {
        return new EventEnvelope<PaymentFailedPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentFailed,
            Timestamp = DateTime.UtcNow,
            Payload = payment.ToFailedPayload()
        };
    }

    // Creates an envelope for a PaymentSucceededPayload
    public static EventEnvelope<PaymentSucceededPayload> ToSucceededEventEnvelope(this Payment payment)
    {
        return new EventEnvelope<PaymentSucceededPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentSucceeded,
            Timestamp = DateTime.UtcNow,
            Payload = payment.ToSucceededPayload()
        };
    }

    // Creates an envelope for a PaymentCancelledPayload
    public static EventEnvelope<PaymentCancelledPayload> ToCancelledEventEnvelope(this Payment payment, string reason = "Payment cancelled")
    {
        return new EventEnvelope<PaymentCancelledPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentCancelled,
            Timestamp = DateTime.UtcNow,
            Payload = payment.ToCancelledPayload(reason)
        };
    }
}


/*
    PaymentEventMapper is a static utility class that converts domain models (Payment)
    into message payloads and envelopes used for RabbitMQ messaging.

    It serves two main purposes:
    1. Maps a Payment object to a specific payload type:
       - e.g., PaymentReservedPayload, PaymentFailedPayload, etc.
       - Includes optional reasons for failures or cancellations.

    2. Wraps those payloads in EventEnvelope<T> objects:
       - Adds metadata like MessageId, EventType, and Timestamp.
       - Ready to be published to RabbitMQ via a publisher service.

    This separation of concerns:
    - Keeps transformation logic out of services and controllers
    - Ensures consistent message structure and formatting
    - Makes the system easier to test, read, and maintain
*/
