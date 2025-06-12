using System.Text.Json;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Events;
using PaymentService.Domain.Mappers;
using PaymentService.Infrastructure.Database;
using PaymentService.Infrastructure.Database.Models;
using PaymentService.Infrastructure.Messaging;

namespace PaymentService.Application.Services;

public class PaymentProcessorService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IPaymentEventPublisher _paymentEventPublisher;
    private readonly ILogger<PaymentProcessorService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PaymentProcessorService(
        IPaymentRepository paymentRepo,
        IPaymentEventPublisher paymentEventPublisher,
        ILogger<PaymentProcessorService> logger,
        JsonSerializerOptions jsonOptions)
    {
        _paymentRepo = paymentRepo;
        _paymentEventPublisher = paymentEventPublisher;
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    public async Task ProcessOrderCreatedAsync(OrderCreatedPayload evt, Guid correlationId)
    {
        _logger.LogInformation("Received OrderCreated event for OrderId: {OrderId} with Event: {Event}", 
            evt.OrderId, JsonSerializer.Serialize(evt, _jsonOptions));

        // Get the total price and currency from the event
        var totalAmount = evt.TotalPrice;
        
        // Validate the currency is provided
        if (string.IsNullOrEmpty(evt.Currency))
        {
            _logger.LogWarning("Missing currency for order {OrderId}", evt.OrderId);
            var failedPayload = new PaymentFailedPayload(
                OrderId: evt.OrderId,
                Reason: "Missing currency information"
            );

            await _paymentEventPublisher.PublishPaymentFailedAsync(failedPayload, evt.OrderId);
            return;
        }

        if (totalAmount <= 0)
        {
            _logger.LogWarning("Invalid payment amount for order {OrderId}", evt.OrderId);
            var failedPayload = new PaymentFailedPayload(
                OrderId: evt.OrderId,
                Reason: "Invalid payment amount"
            );

            await _paymentEventPublisher.PublishPaymentFailedAsync(failedPayload, evt.OrderId);
            return;
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            OrderId = evt.OrderId,
            Amount = totalAmount,
            Currency = evt.Currency, 
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Status = PaymentStatus.Reserved
        };

        _logger.LogInformation("Processing payment for order {OrderId} with amount {Amount} {Currency}", 
            evt.OrderId, totalAmount, evt.Currency);

        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();
        _logger.LogInformation("Payment reserved for order {OrderId}", evt.OrderId);

        var reservedPayload = payment.ToReservedPayload();
        await _paymentEventPublisher.PublishPaymentReservedAsync(reservedPayload, correlationId);
        _logger.LogInformation("Published PaymentReservedEvent for order {OrderId}", payment.OrderId);
    }

    public async Task ProcessRestaurantRejectedAsync(RestaurantRejectedPayload evt, Guid correlationId)
    {
        _logger.LogInformation("Received RestaurantRejected event for OrderId: {OrderId} with Event: {Event}", 
            evt.OrderId, JsonSerializer.Serialize(evt, _jsonOptions));

        // Retrieve existing payment based on OrderId
        var payment = await _paymentRepo.GetByOrderIdAsync(evt.OrderId);
        _logger.LogInformation("Retrieved payment for OrderId {OrderId}: {Payment}", 
            evt.OrderId, payment is not null ? JsonSerializer.Serialize(payment, _jsonOptions) : "null");

        // Log and stop if payment doesn't exist
        if (payment is null)
        {
            _logger.LogWarning("No payment found for OrderId {OrderId}", evt.OrderId);
            return;
        }

        // Update status to Cancelled
        payment.Status = PaymentStatus.Cancelled;
        payment.LastUpdated = DateTime.UtcNow;

        // Save changes to database
        await _paymentRepo.UpdateAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        // Create and publish a PaymentCancelledEvent
        var cancelledPayload = payment.ToCancelledPayload($"Restaurant rejected order: {evt.Reason}");
        await _paymentEventPublisher.PublishPaymentCancelledAsync(cancelledPayload, correlationId);

        _logger.LogInformation("Payment cancelled for order {OrderId} with Reason: {Reason}", 
            evt.OrderId, evt.Reason);
    }

    public async Task FinalizePaymentAsync(DeliveryStartedPayload evt, Guid correlationId)
    {
        _logger.LogInformation("Received DeliveryStarted event for OrderId: {OrderId} with Event: {Event}", 
            evt.OrderId, JsonSerializer.Serialize(evt, _jsonOptions));

        // Retrieve existing payment based on OrderId
        var payment = await _paymentRepo.GetByOrderIdAsync(evt.OrderId);
        _logger.LogInformation("Retrieved payment for OrderId {OrderId}: {Payment}", 
            evt.OrderId, JsonSerializer.Serialize(payment, _jsonOptions));

        // Log and stop if payment doesn't exist
        if (payment is null)
        {
            _logger.LogWarning("No payment found for OrderId {OrderId}", evt.OrderId);
            return;
        }

        // Update status to Succeeded
        payment.Status = PaymentStatus.Succeeded;
        payment.LastUpdated = DateTime.UtcNow;

        // Save changes to database
        await _paymentRepo.UpdateAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        // Create and publish a PaymentSucceededEvent
        var succeededPayload = payment.ToSucceededPayload();
        await _paymentEventPublisher.PublishPaymentSucceededAsync(succeededPayload, correlationId);

        _logger.LogInformation("Payment finalized for order {OrderId}", evt.OrderId);
    }
}