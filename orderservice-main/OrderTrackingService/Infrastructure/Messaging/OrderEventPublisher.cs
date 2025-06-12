using OrderTrackingService.Domain;
using OrderTrackingService.Domain.Events;

namespace OrderTrackingService.Infrastructure.Messaging;
    
public class RabbitMqOrderEventPublisher(
    IMessagePublisher publisher)
    : IOrderEventPublisher
{
    public async Task PublishOrderCreatedAsync(EventEnvelope<OrderCreatedPayload> eventEnvelope, Guid correlationId)
    {
        await publisher.PublishAsync(RabbitMqTopology.EventExchange, RabbitMqTopology.PublishOrderCreated, correlationId, eventEnvelope);
    }
}

public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(EventEnvelope<OrderCreatedPayload> eventEnvelope, Guid correlationId);
}


