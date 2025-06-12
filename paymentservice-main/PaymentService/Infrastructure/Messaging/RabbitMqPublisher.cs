using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PaymentService.Application;
using PaymentService.Domain;
using RabbitMQ.Client;

namespace PaymentService.Infrastructure.Messaging;

public class RabbitMqMessagePublisher(
    IConnection rabbitConnection,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<RabbitMqMessagePublisher> logger)
    : IMessagePublisher
{
    private const ushort MaxOutstandingConfirms = 256;
    private const int MaxPublishAttempts = 3;
    private TimeSpan _delay = TimeSpan.FromSeconds(1);

    public async Task PublishAsync(string exchange, string routingKey, Guid correlationId, object message)
    {
        var channelOpts = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(MaxOutstandingConfirms)
        );
        // Create a separate channel for publishing
        await using var channel = await rabbitConnection.CreateChannelAsync(channelOpts);
        
        var payload = JsonSerializer.Serialize(message, jsonSerializerOptions);
        var body = Encoding.UTF8.GetBytes(payload);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            CorrelationId = correlationId.ToString()
        };
        
        var activity = Activity.Current ?? new Activity("RabbitMqPublish");
        TracingHelper.InjectContextIntoProperties(props, activity);

        logger.LogInformation("Publishing message to exchange: {Exchange}, routingKey: {RoutingKey}, message: {Message}", 
            exchange, routingKey, payload);
        
        for (var attempt = 1; attempt <= MaxPublishAttempts; attempt++)
        {
            try
            {
                await channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: props,
                    body: body,
                    mandatory: false
                );

                logger.LogInformation("Message confirmed by broker on attempt #{Attempt}", attempt);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Publish attempt #{Attempt} failed", attempt);

                if (attempt == MaxPublishAttempts)
                {
                    logger.LogError("Exceeded max publish attempts ({MaxAttempts}), routing to DLQ", MaxPublishAttempts);
                    await PublishToDlqAsync(props, body);
                    throw new Exception($"Could not publish message after {MaxPublishAttempts} attempts");
                }
                
                logger.LogWarning("Waiting {Delay} before retry #{NextAttempt}", _delay, attempt + 1);
                await Task.Delay(_delay); 
                _delay *= 2; // exponential back-off
            }
        }
    }
    
    public async Task PublishToDlqAsync(IBasicProperties props, ReadOnlyMemory<byte> body)
    {
        var dlqProps = new BasicProperties
        {
            AppId          = props.AppId,
            MessageId      = props.MessageId,
            UserId         = props.UserId,
            Type           = props.Type,
            Expiration     = props.Expiration,
            Priority       = props.Priority,
            Timestamp      = props.Timestamp,
            ReplyTo        = props.ReplyTo,
            DeliveryMode   = props.DeliveryMode
        };
        
        await using var channel = await rabbitConnection.CreateChannelAsync();
        
        await channel.BasicPublishAsync(
            exchange: RabbitMqTopology.DefaultExchange,
            routingKey: RabbitMqTopology.DeadLetterQueue,
            basicProperties: dlqProps,
            body: body,
            mandatory: false
        );
        
        logger.LogInformation("Published message to DLQ '{Dlq}'", RabbitMqTopology.DeadLetterQueue);
    }
}