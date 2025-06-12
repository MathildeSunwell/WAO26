namespace PaymentService.Infrastructure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(string exchange, string routingKey, Guid correlationId, object message);
}
