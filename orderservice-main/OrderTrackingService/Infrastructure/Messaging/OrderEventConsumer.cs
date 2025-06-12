using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OrderTrackingService.Application;
using OrderTrackingService.Domain;
using OrderTrackingService.Domain.Enums;
using OrderTrackingService.Domain.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace OrderTrackingService.Infrastructure.Messaging;

public class OrderEventConsumer(
    IChannel channel, 
    IServiceScopeFactory scopes, 
    JsonSerializerOptions jsonOptions,
    ILogger<OrderEventConsumer> logger)
    : BackgroundService
{
    private const int MaxRetries = 3;
    private static readonly Dictionary<OrderEventType, Type> _payloadTypeMap = new()
    {
        { OrderEventType.OrderCreated, typeof(OrderCreatedPayload) },
        { OrderEventType.PaymentReserved, typeof(PaymentReservedPayload) },
        { OrderEventType.PaymentSucceeded, typeof(PaymentSucceededPayload) },
        { OrderEventType.PaymentFailed, typeof(PaymentFailedPayload) },
        { OrderEventType.PaymentCancelled, typeof(PaymentCancelledPayload) },
        { OrderEventType.RestaurantAccepted, typeof(RestaurantAcceptedPayload) },
        { OrderEventType.RestaurantRejected, typeof(RestaurantRejectedPayload) },
        { OrderEventType.RestaurantOrderReady, typeof(RestaurantOrderReadyPayload) },
        { OrderEventType.RestaurantCancelled, typeof(RestaurantCancelledPayload) },
        { OrderEventType.DeliveryAssigned, typeof(DeliveryAssignedPayload) },
        { OrderEventType.DeliveryStarted, typeof(DeliveryStartedPayload) },
        { OrderEventType.DeliveryCompleted, typeof(DeliveryCompletedPayload) },
        { OrderEventType.DeliveryCancelled, typeof(DeliveryCancelledPayload) }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string consumerTag = null;

            try
            {
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += OnMessageReceived;

                consumerTag = await channel.BasicConsumeAsync(
                    queue: RabbitMqTopology.EventQueue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.LogInformation("Subscribed to queue '{Queue}' (tag={Tag})",
                    RabbitMqTopology.EventQueue, consumerTag);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Cancellation requested, stopping consumer loop");
                if (consumerTag is not null)
                {
                    try
                    {
                        await channel.BasicCancelAsync(consumerTag); // does not pass cancellation token on already cancelled consumer
                        logger.LogInformation("Cancelled consumer (tag={Tag})", consumerTag);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error cancelling consumer (tag={Tag})", consumerTag);
                    }
                }
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Consumer loop failed—will retry in 5s");
                try
                {
                    if (consumerTag is not null)
                    {
                        await channel.BasicCancelAsync(consumerTag); // does not pass cancellation token on already cancelled consumer
                    }
                }
                catch (Exception cancelEx)
                {
                    logger.LogWarning(cancelEx, "Error during cleanup of failed consumer");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
    
    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        using var activity = TracingHelper.StartConsumerActivity(eventArgs, "OrderEventConsumer");
        var correlationId = Guid.Parse(eventArgs.BasicProperties.CorrelationId!);
        using var _ = LogContext.PushProperty("CorrelationId", correlationId);
        
        var props = eventArgs.BasicProperties;
        var headers= props?.Headers;
        if (headers != null && headers.TryGetValue("traceparent", out var rawTp))
        {
            // RabbitMQ gives you the header as a byte[] of the ASCII hex
            string traceParent = rawTp is byte[] bytes
                ? Encoding.UTF8.GetString(bytes)
                : rawTp.ToString();

            logger.LogInformation("Incoming W3C traceparent: {TraceParent}", traceParent);
        }
        else
        {
            logger.LogWarning("No traceparent header found on this message.");
        }
        logger.LogInformation("Activity started for message processing: {ActivityId}", Activity.Current.Id);
        var retryCount = GetRetryCount(headers);
        logger.LogInformation(
            "Received DeliveryTag={Tag}, Exchange={Exchange}, RoutingKey={RoutingKey}, Redelivered={Redelivered}, retryCount={RetryCount}",
            eventArgs.DeliveryTag, eventArgs.Exchange, eventArgs.RoutingKey, eventArgs.Redelivered, retryCount);
        
        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var raw  = JsonSerializer.Deserialize<RawEventEnvelope>(json, jsonOptions)
                       ?? throw new InvalidOperationException("Envelope null");
            
            var payloadType = _payloadTypeMap[raw.EventType];
            var closedType  = typeof(EventEnvelope<>).MakeGenericType(payloadType);
            var envelope    = JsonSerializer.Deserialize(json, closedType, jsonOptions)
                              ?? throw new InvalidOperationException("Payload null");

            await ProcessOrderAsync(envelope, raw.EventType, raw.CorrelationId);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in parse/process (retryCount={RetryCount})", retryCount);

            if (retryCount < MaxRetries)
            {
                logger.LogWarning("Transient failure; retrying attempt #{Attempt}", retryCount + 1);
                await channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false);
            }
            else
            {
                logger.LogWarning("MaxRetries exceeded; publishing to DLQ manually");
                using var scope = scopes.CreateScope();
                var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
                await messagePublisher.PublishToDlqAsync((IBasicProperties)props, eventArgs.Body);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
        }
    }
    
    private async Task ProcessOrderAsync(object env, OrderEventType eventType, Guid correlationId)
    {
        dynamic envelope = env;
        
        using var scope = scopes.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        
        logger.LogInformation("Processing order event type: {eventType}", eventType);

        switch (eventType)
        {
            case OrderEventType.RestaurantAccepted:
                await orderService.ProcessRestaurantAcceptedAsync(envelope, correlationId);
                break;
            case OrderEventType.RestaurantRejected:
                await orderService.ProcessRestaurantRejectedAsync(envelope, correlationId);
                break;
            case OrderEventType.RestaurantOrderReady:
                await orderService.ProcessRestaurantOrderReadyAsync(envelope, correlationId);
                break;
            case OrderEventType.RestaurantCancelled:
                await orderService.ProcessRestaurantCancelledAsync(envelope, correlationId);
                break;
            case OrderEventType.PaymentReserved:
                await orderService.ProcessPaymentReservedAsync(envelope, correlationId);
                break;
            case OrderEventType.PaymentFailed:
                await orderService.ProcessPaymentFailedAsync(envelope, correlationId);
                break;
            case OrderEventType.PaymentSucceeded:
                await orderService.ProcessPaymentSucceededAsync(envelope, correlationId);
                break;
            case OrderEventType.PaymentCancelled:
                await orderService.ProcessPaymentCancelledAsync(envelope, correlationId);
                break;
            case OrderEventType.DeliveryAssigned:
                await orderService.ProcessDeliveryAssignedAsync(envelope, correlationId);
                break;
            case OrderEventType.DeliveryStarted:
                await orderService.ProcessDeliveryStartedAsync(envelope, correlationId);
                break;
            case OrderEventType.DeliveryCompleted:
                await orderService.ProcessDeliveryCompletedAsync(envelope, correlationId);
                break;
            case OrderEventType.DeliveryCancelled:
                await orderService.ProcessDeliveryCancelledAsync(envelope, correlationId);
                break;
            case OrderEventType.OrderCreated:
            default:
                logger.LogWarning("Unknown event type: {eventType}", eventType);
                break;
        }
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