using System.Text.Json;
using PaymentService.Domain;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Events;

namespace PaymentService.Infrastructure.Messaging;

public class RabbitMqPaymentEventPublisher : IPaymentEventPublisher
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<RabbitMqPaymentEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMqPaymentEventPublisher(IMessagePublisher publisher, ILogger<RabbitMqPaymentEventPublisher> logger, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    public async Task PublishPaymentReservedAsync(PaymentReservedPayload paymentEvent, Guid correlationId)
    {

        // Create an envelope for the payment event
        var envelope = new EventEnvelope<PaymentReservedPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentReserved,     // Set the event type enum
            Timestamp = DateTime.UtcNow,
            Payload = paymentEvent                          // Set the payload to the payment event  
        };

        _logger.LogInformation("Publishing PaymentReserved event for OrderId: {OrderId} with Event: {Event}",
            paymentEvent.OrderId, JsonSerializer.Serialize(envelope, _jsonOptions));

        // Publish the event to the RabbitMQ exchange with routing key
        await _publisher.PublishAsync(
            RabbitMqTopology.EventExchange,                 // Use the main event exchange (events.topic)
            RabbitMqTopology.PaymentReservedKey,            // Use the routing key for payment reserved events (payment.reserved)
            correlationId,
            envelope);
    }

    public async Task PublishPaymentFailedAsync(PaymentFailedPayload paymentEvent, Guid correlationId)
    {
        var envelope = new EventEnvelope<PaymentFailedPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentFailed,
            Timestamp = DateTime.UtcNow,
            Payload = paymentEvent
        };

        _logger.LogInformation("Publishing PaymentFailed event for OrderId: {OrderId} with Event: {Event}",
            paymentEvent.OrderId, JsonSerializer.Serialize(envelope, _jsonOptions));

        await _publisher.PublishAsync(
            RabbitMqTopology.EventExchange,
            RabbitMqTopology.PaymentFailedKey,
            correlationId,
            envelope);
    }

    public async Task PublishPaymentSucceededAsync(PaymentSucceededPayload paymentEvent, Guid correlationId)
    {
        var envelope = new EventEnvelope<PaymentSucceededPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentSucceeded,
            Timestamp = DateTime.UtcNow,
            Payload = paymentEvent
        };

        _logger.LogInformation("Publishing PaymentSucceeded event for OrderId: {OrderId} with Event: {Event}",
            paymentEvent.OrderId, JsonSerializer.Serialize(envelope, _jsonOptions));

        await _publisher.PublishAsync(
            RabbitMqTopology.EventExchange,
            RabbitMqTopology.PaymentSucceededKey,
            correlationId,
            envelope);
    }

    public async Task PublishPaymentCancelledAsync(PaymentCancelledPayload paymentEvent, Guid correlationId)
    {
        var envelope = new EventEnvelope<PaymentCancelledPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentCancelled,
            Timestamp = DateTime.UtcNow,
            Payload = paymentEvent
        };

        _logger.LogInformation("Publishing PaymentCancelled event for OrderId: {OrderId} with Event: {Event}",
            paymentEvent.OrderId, JsonSerializer.Serialize(envelope, _jsonOptions));

        await _publisher.PublishAsync(
            RabbitMqTopology.EventExchange,
            RabbitMqTopology.PaymentCancelledKey,
            correlationId,
            envelope);
    }
}


/*
    RabbitMqPaymentEventPublisher is responsible for publishing domain events related to payment outcomes.

    It wraps each event (e.g., PaymentReserved, PaymentFailed) in a EventEnvelope,
    including metadata such as message ID, timestamp, event type, and the original payload.

    It then sends the event to RabbitMQ using a predefined exchange and routing key
    by delegating the actual publishing to the injected IMessagePublisher.

    The class also logs each publishing operation with the order ID and full event content for observability.

    This approach ensures all outgoing events follow a consistent structure, support correlation/tracing,
    and can be routed properly through RabbitMQ for consumption by other services.
*/

