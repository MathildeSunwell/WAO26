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
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private static readonly ActivitySource ActivitySource = new ActivitySource("PaymentService.Messaging");

    public RabbitMqPaymentEventConsumer(IChannel channel, IServiceScopeFactory scopes, JsonSerializerOptions jsonOptions, ILogger<RabbitMqPaymentEventConsumer> logger)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        _channel = channel;
        _scopes = scopes;
        _jsonOptions = jsonOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting payment service message consumer");
        
        try
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceived;
            
            var consumerTag = await _channel.BasicConsumeAsync(
                queue: RabbitMqTopology.EventQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);
                
            _logger.LogInformation("Subscribed to queue '{Queue}' (tag={Tag})", 
                RabbitMqTopology.EventQueue, consumerTag);

            // Keep the service running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            
            // Clean shutdown
            _logger.LogInformation("Cancellation requested, stopping consumer");
            await _channel.BasicCancelAsync(consumerTag, cancellationToken: CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, don't report as error
            _logger.LogInformation("Consumer shutdown complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message consumer");
        }
    }
    
    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        using var activity = TracingHelper.StartConsumerActivity(eventArgs, "EventConsumer");
        var correlationId = Guid.TryParse(eventArgs.BasicProperties.CorrelationId, out var corrId) 
            ? corrId 
            : Guid.NewGuid();
        using var correlation = LogContext.PushProperty("CorrelationId", correlationId);

        var props = eventArgs.BasicProperties;
        var headers = props?.Headers;
        if (headers != null && headers.TryGetValue("traceparent", out var rawTp))
        {
            var traceParent = rawTp is byte[] bytes
                ? Encoding.UTF8.GetString(bytes)
                : rawTp.ToString();

            _logger.LogInformation("Incoming W3C traceparent: {TraceParent}", traceParent);
        }
        else
        {
            _logger.LogWarning("No traceparent header found on this message.");
        }
        _logger.LogInformation("Activity started for message processing: {ActivityId}", Activity.Current.Id);
        var retryCount = GetRetryCount(headers);
        _logger.LogInformation(
            "Received DeliveryTag={Tag}, Exchange={Exchange}, RoutingKey={RoutingKey}, Redelivered={Redelivered}, retryCount={RetryCount}",
            eventArgs.DeliveryTag, eventArgs.Exchange, eventArgs.RoutingKey, eventArgs.Redelivered, retryCount);

        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var raw  = JsonSerializer.Deserialize<RawEventEnvelope>(json, _jsonOptions)
                       ?? throw new InvalidOperationException("Envelope null");
            
            await ProcessMessageAsync(raw.EventType, json, correlationId);

            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in parse/process (retryCount={RetryCount})", retryCount);

            if (retryCount < MaxRetries)
            {
                _logger.LogWarning("Transient failure; retrying attempt #{Attempt}", retryCount + 1);
                await _channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);
            }
            else
            {
                _logger.LogWarning("MaxRetries exceeded; publishing to DLQ manually");
                using var scope = _scopes.CreateScope();
                var messagePublisher = scope.ServiceProvider.GetRequiredService<RabbitMqMessagePublisher>();
                await messagePublisher.PublishToDlqAsync((IBasicProperties)props, eventArgs.Body);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
        }
    }

    private async Task ProcessMessageAsync(OrderEventType eventType, string json, Guid correlationId)
    {
        using var scope = _scopes.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        try
        {
            _logger.LogInformation("Processing {EventType} event with {payload}", eventType, json);

            switch (eventType)
            {
                case OrderEventType.OrderCreated:
                    var createdEnvelope = JsonSerializer.Deserialize<EventEnvelope<OrderCreatedPayload>>(json, _jsonOptions);
                    if (createdEnvelope?.Payload == null)
                    {
                        throw new InvalidOperationException("Could not deserialize OrderCreatedPayload envelope");
                    }
                    await paymentService.ProcessOrderCreatedAsync(createdEnvelope.Payload, correlationId);
                    break;

                case OrderEventType.RestaurantRejected:
                    var rejectedEnvelope = JsonSerializer.Deserialize<EventEnvelope<RestaurantRejectedPayload>>(json, _jsonOptions);
                    if (rejectedEnvelope?.Payload == null)
                    {
                        throw new InvalidOperationException("Could not deserialize RestaurantRejectedPayload envelope");
                    }
                    await paymentService.ProcessRestaurantRejectedAsync(rejectedEnvelope.Payload, correlationId);
                    break;

                case OrderEventType.DeliveryStarted:
                    var deliveryStartedEnvelope = JsonSerializer.Deserialize<EventEnvelope<DeliveryStartedPayload>>(json, _jsonOptions);
                    if (deliveryStartedEnvelope?.Payload == null)
                    {
                        throw new InvalidOperationException("Could not deserialize DeliveryStartedPayload envelope");
                    }
                    await paymentService.FinalizePaymentAsync(deliveryStartedEnvelope.Payload, correlationId);
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {eventType}", eventType);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message: {Json}", json);
            throw;
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
    
    private static int GetRetryCount(IDictionary<string, object> headers)
    {
        if (headers == null || !headers.TryGetValue("x-death", out var deathObj))
            return 0;

        var deaths = (IList<object>)deathObj;
        if (deaths.Count == 0) return 0;

        var firstDeath = (IDictionary<string, object>)deaths[0];
        return Convert.ToInt32(firstDeath["count"]);
    }
}