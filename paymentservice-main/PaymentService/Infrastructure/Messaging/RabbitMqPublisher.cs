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
    private const ushort MaxOutstandingConfirms = 256;       // Maximum number of unconfirmed messages allowed
    private const int MaxPublishAttempts = 3;                // Maximum number of publish attempts
    private TimeSpan _delay = TimeSpan.FromSeconds(1);       // Delay between publish attempts  

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
            Persistent = true,                              // Save messages to disk
            ContentType = "application/json",
            CorrelationId = correlationId.ToString()        // Set correlation ID for tracing
        };

        var activity = Activity.Current ?? new Activity("RabbitMqPublish");         // Use current activity or create a new one
        TracingHelper.InjectContextIntoProperties(props, activity);                 // Inject tracing context into Rabbitmq message headers

        logger.LogInformation("Publishing message to exchange: {Exchange}, routingKey: {RoutingKey}, message: {Message}",
            exchange, routingKey, payload);

        for (var attempt = 1; attempt <= MaxPublishAttempts; attempt++)             // Retry logic for publishing
        {
            try
            {
                await channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: props,
                    body: body,
                    mandatory: false                 // if no queue matches, message will not be returned
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
                    // If we reach max attempts, publish to dead-letter queue
                    await PublishToDlqAsync(props, body);
                    // Rethrow the exception to notify the caller
                    throw new Exception($"Could not publish message after {MaxPublishAttempts} attempts");
                }

                logger.LogWarning("Waiting {Delay} before retry #{NextAttempt}", _delay, attempt + 1);
                await Task.Delay(_delay);
                _delay *= 2;
            }
        }
    }

    public async Task PublishToDlqAsync(IBasicProperties props, ReadOnlyMemory<byte> body)
    {
        // Publish the message to the dead-letter queue with the same properties
        var dlqProps = new BasicProperties
        {
            AppId = props.AppId,
            MessageId = props.MessageId,
            UserId = props.UserId,
            Type = props.Type,
            Expiration = props.Expiration,
            Priority = props.Priority,
            Timestamp = props.Timestamp,
            ReplyTo = props.ReplyTo,
            DeliveryMode = props.DeliveryMode
        };

        await using var channel = await rabbitConnection.CreateChannelAsync();

        // Ensure the dead-letter queue is declared
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



/*
    This class is responsible for publishing messages to RabbitMQ with support for:
    - Structured message properties including correlationId and tracing (Activity)
    - Retry logic with exponential backoff (up to 3 attempts)
    - Fallback to a Dead Letter Queue (DLQ) if all publish attempts fail

    Key steps:
    1. Create a dedicated channel with publisher confirmations enabled
    2. Serialize the message and prepare RabbitMQ BasicProperties
    3. Inject OpenTelemetry trace context (traceparent) into message headers
    4. Publish the message to the specified exchange and routing key
    5. On failure, retry with increasing delay
    6. If all retries fail, route the message to a DLQ for manual inspection
*/

