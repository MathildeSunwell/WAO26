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
        var envelope = new EventEnvelope<PaymentReservedPayload>
        {
            MessageId = Guid.NewGuid(),
            EventType = OrderEventType.PaymentReserved,
            Timestamp = DateTime.UtcNow,
            Payload = paymentEvent
        };

        _logger.LogInformation("Publishing PaymentReserved event for OrderId: {OrderId} with Event: {Event}", 
            paymentEvent.OrderId, JsonSerializer.Serialize(envelope, _jsonOptions));

        await _publisher.PublishAsync(
            RabbitMqTopology.EventExchange, 
            RabbitMqTopology.PaymentReservedKey, 
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