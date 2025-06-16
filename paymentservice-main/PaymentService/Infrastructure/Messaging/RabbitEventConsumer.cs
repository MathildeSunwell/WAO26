using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using PaymentService.Application;
using PaymentService.Application.Services;
using PaymentService.Domain;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace PaymentService.Infrastructure.Messaging;

public class RabbitMqPaymentEventConsumer : BackgroundService   
{
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopes;                    
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<RabbitMqPaymentEventConsumer> _logger;   
    private const int MaxRetries = 3;

    // Propagator is used to extract and inject context in message headers
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator; 

    // ActivitySource is used to create new activities for tracing                  
    private static readonly ActivitySource ActivitySource = new ActivitySource("PaymentService.Messaging");         

    public RabbitMqPaymentEventConsumer(IChannel channel, IServiceScopeFactory scopes, JsonSerializerOptions jsonOptions, ILogger<RabbitMqPaymentEventConsumer> logger)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;    

        _channel = channel;
        _scopes = scopes;
        _jsonOptions = jsonOptions;
        _logger = logger;
    }

    // This method is called when the service starts
    // It sets up the RabbitMQ consumer and starts listening for messages
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting payment service message consumer");

        try
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceived;                // Attach the message handler - when a message is received, this method will be called

            // Declare the queue and exchange if they don't exist
            var consumerTag = await _channel.BasicConsumeAsync(
                queue: RabbitMqTopology.EventQueue,
                autoAck: false,                                         // Messages must be acknowledged manually 
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Subscribed to queue '{Queue}' (tag={Tag})",
                RabbitMqTopology.EventQueue, consumerTag);

            // Keep the service running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            // When stoppingToken is triggered, we cancel the consumer
            _logger.LogInformation("Cancellation requested, stopping consumer");
            await _channel.BasicCancelAsync(consumerTag, cancellationToken: CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer shutdown complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message consumer");
        }
    }
    
    // This method is called when a message is received from RabbitMQ
    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        // Uses traceparent from headers if available, or starts a new span
        using var activity = TracingHelper.StartConsumerActivity(eventArgs, "EventConsumer");

        // Extract correlationId from message - If not present, generate a new one
        var correlationId = Guid.TryParse(eventArgs.BasicProperties.CorrelationId, out var corrId)
            ? corrId
            : Guid.NewGuid();

        // Add correlationId as structured log context 
        using var correlation = LogContext.PushProperty("CorrelationId", correlationId);

        // Extract message properties and headers
        var props = eventArgs.BasicProperties;
        var headers = props?.Headers;

        // Check if the traceparent header exists - If yes, decode it and log the trace ID
        if (headers != null && headers.TryGetValue("traceparent", out var rawTp))
        {
            var traceParent = rawTp is byte[] bytes
                ? Encoding.UTF8.GetString(bytes)
                : rawTp.ToString();

            _logger.LogInformation("Incoming W3C traceparent: {TraceParent}", traceParent);
        }
        else
        {
            // Warn if no tracing information was found 
            _logger.LogWarning("No traceparent header found on this message.");
        }

        // Log that message handling has begun and show the activity/span ID
        _logger.LogInformation("Activity started for message processing: {ActivityId}", Activity.Current.Id);

        // Check how many times this message has already failed 
        var retryCount = GetRetryCount(headers);

        _logger.LogInformation(
            "Received DeliveryTag={Tag}, Exchange={Exchange}, RoutingKey={RoutingKey}, Redelivered={Redelivered}, retryCount={RetryCount}",
            eventArgs.DeliveryTag, eventArgs.Exchange, eventArgs.RoutingKey, eventArgs.Redelivered, retryCount);

        try
        {
            // Convert byte[] body to string
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            // Deserialize to a generic envelope to access EventType
            var raw = JsonSerializer.Deserialize<RawEventEnvelope>(json, _jsonOptions)
                    ?? throw new InvalidOperationException("Envelope null");

            // Forward event type and raw JSON to processor method
            await ProcessMessageAsync(raw.EventType, json, correlationId);

            // Acknowledge successful handling back to RabbitMQ
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            // Log the exception and retry state
            _logger.LogError(ex, "Error in parse/process (retryCount={RetryCount})", retryCount);

            if (retryCount < MaxRetries)
            {
                // If under retry limit, reject the message without requeue
                // It will be routed to a retry queue (via dead-letter)
                _logger.LogWarning("Transient failure; retrying attempt #{Attempt}", retryCount + 1);
                await _channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);
            }
            else
            {
                // If retries exceeded, publish manually to DLQ for analysis
                _logger.LogWarning("MaxRetries exceeded; publishing to DLQ manually");

                // Create a scoped service provider to get the message publisher
                using var scope = _scopes.CreateScope();
                var messagePublisher = scope.ServiceProvider.GetRequiredService<RabbitMqMessagePublisher>();

                // Send the message to a dead-letter queue
                await messagePublisher.PublishToDlqAsync((IBasicProperties)props, eventArgs.Body);

                // Acknowledge message to remove it from the main queue
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
        }
    }


    // This method processes the incoming message based on its event type.
    private async Task ProcessMessageAsync(OrderEventType eventType, string json, Guid correlationId)
    {
        using var scope = _scopes.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        try
        {
            // Log which type of event is being processed and the raw JSON payload
            _logger.LogInformation("Processing {EventType} event with {payload}", eventType, json);

            // Choose how to handle the event based on its type
            switch (eventType)
            {
                case OrderEventType.OrderCreated:
                    // Deserialize the message to the expected payload type
                    var createdEnvelope = JsonSerializer.Deserialize<EventEnvelope<OrderCreatedPayload>>(json, _jsonOptions);

                    // Validate the payload exists after deserialization
                    if (createdEnvelope?.Payload == null)
                    {
                        throw new InvalidOperationException("Could not deserialize OrderCreatedPayload envelope");
                    }

                    // Call domain logic (IPaymentService) for order creation
                    await paymentService.ProcessOrderCreatedAsync(createdEnvelope.Payload, correlationId);
                    break;

                case OrderEventType.RestaurantRejected:
                    // Deserialize to the rejected payload 
                    var rejectedEnvelope = JsonSerializer.Deserialize<EventEnvelope<RestaurantRejectedPayload>>(json, _jsonOptions);
                    if (rejectedEnvelope?.Payload == null)
                    {
                        throw new InvalidOperationException("Could not deserialize RestaurantRejectedPayload envelope");
                    }

                    // Process the rejection (cancel payment)
                    await paymentService.ProcessRestaurantRejectedAsync(rejectedEnvelope.Payload, correlationId);
                    break;

                case OrderEventType.DeliveryStarted:
                    // Deserialize to the delivery-started payload
                    var deliveryStartedEnvelope = JsonSerializer.Deserialize<EventEnvelope<DeliveryStartedPayload>>(json, _jsonOptions);
                    if (deliveryStartedEnvelope?.Payload == null)
                    {
                        throw new InvalidOperationException("Could not deserialize DeliveryStartedPayload envelope");
                    }

                    // Finalize the payment when delivery starts
                    await paymentService.FinalizePaymentAsync(deliveryStartedEnvelope.Payload, correlationId);
                    break;

                default:
                    // If the event type is unknown, log a warning 
                    _logger.LogWarning("Unknown event type: {eventType}", eventType);
                    break;
            }
        }
        catch (JsonException ex)
        {
            // Catch and log any deserialization errors
            _logger.LogError(ex, "Failed to deserialize message: {Json}", json);
            throw;
        }
    }


    public override void Dispose()
    {
        _channel?.Dispose(); // Ensure RabbitMQ channel is closed and disposed properly
        base.Dispose();      // Call base dispose to complete cleanup
    }

    
    private static int GetRetryCount(IDictionary<string, object> headers)
    {
        // If no headers or x-death header is missing, this is the first attempt
        if (headers == null || !headers.TryGetValue("x-death", out var deathObj))
            return 0;

        // x-death header is a list – each entry describes a failed delivery
        var deaths = (IList<object>)deathObj;
        if (deaths.Count == 0) return 0;

        // Extract count from the first x-death record (number of delivery attempts)
        var firstDeath = (IDictionary<string, object>)deaths[0];
        return Convert.ToInt32(firstDeath["count"]);
    }

}