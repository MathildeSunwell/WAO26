using System.Text.Json;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Events;
using PaymentService.Domain.Mappers;
using PaymentService.Infrastructure.Database;
using PaymentService.Infrastructure.Database.Models;
using PaymentService.Infrastructure.Messaging;

namespace PaymentService.Application.Services;

// CORE BUSINESS LOGIC: This service handles the entire payment workflow
// It implements the IPaymentService interface and processes 3 main events:
// 1. OrderCreated -> Reserve payment
// 2. RestaurantRejected -> Cancel payment  
// 3. DeliveryStarted -> Finalize payment

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

    // EVENT HANDLER 1: Customer creates an order -> Reserve payment
    // This is called when RabbitMQ receives an "OrderCreated" event
    public async Task ProcessOrderCreatedAsync(OrderCreatedPayload evt, Guid correlationId)
    {
        // LOGGING: Always log incoming events for observability
        _logger.LogInformation("Received OrderCreated event for OrderId: {OrderId} with Event: {Event}",
            evt.OrderId, JsonSerializer.Serialize(evt, _jsonOptions));

        // EXTRACT DATA: Get payment details from the order event
        var totalAmount = evt.TotalPrice;

        // VALIDATION 1: Check if currency is provided
        if (string.IsNullOrEmpty(evt.Currency))
        {
            _logger.LogWarning("Missing currency for order {OrderId}", evt.OrderId);

            // EARLY RETURN: Publish failure event and stop processing
            var failedPayload = new PaymentFailedPayload(
                OrderId: evt.OrderId,
                Reason: "Missing currency information"
            );

            await _paymentEventPublisher.PublishPaymentFailedAsync(failedPayload, evt.OrderId);
            return;
        }

        // VALIDATION 2: Check if amount is valid
        if (totalAmount <= 0)
        {
            _logger.LogWarning("Invalid payment amount for order {OrderId}", evt.OrderId);

            // EARLY RETURN: Publish failure event and stop processing
            var failedPayload = new PaymentFailedPayload(
                OrderId: evt.OrderId,
                Reason: "Invalid payment amount"
            );

            await _paymentEventPublisher.PublishPaymentFailedAsync(failedPayload, evt.OrderId);
            return;
        }

        // CREATE PAYMENT: Build new payment entity with Reserved status
        var payment = new Payment
        {
            Id = Guid.NewGuid(),                    
            CorrelationId = correlationId,          
            OrderId = evt.OrderId,   // Link to order
            Amount = totalAmount,                   
            Currency = evt.Currency,                
            CreatedAt = DateTime.UtcNow,           
            LastUpdated = DateTime.UtcNow,         
            Status = PaymentStatus.Reserved        
        };

        _logger.LogInformation("Processing payment for order {OrderId} with amount {Amount} {Currency}",
            evt.OrderId, totalAmount, evt.Currency);

        // SAVE TO DATABASE: Persist the payment record
        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();
        _logger.LogInformation("Payment reserved for order {OrderId}", evt.OrderId);

        // PUBLISH EVENT: Tell other services payment is reserved
        var reservedPayload = payment.ToReservedPayload(); // Uses mapper extension method
        await _paymentEventPublisher.PublishPaymentReservedAsync(reservedPayload, correlationId);
        _logger.LogInformation("Published PaymentReservedEvent for order {OrderId}", payment.OrderId);
    }

    // EVENT HANDLER 2: Restaurant rejects order -> Cancel payment
    // This is called when RabbitMQ receives a "RestaurantRejected" event
    public async Task ProcessRestaurantRejectedAsync(RestaurantRejectedPayload evt, Guid correlationId)
    {
        // LOGGING: Track the incoming rejection event
        _logger.LogInformation("Received RestaurantRejected event for OrderId: {OrderId} with Event: {Event}",
            evt.OrderId, JsonSerializer.Serialize(evt, _jsonOptions));

        // FIND PAYMENT: Look up existing payment by OrderId
        var payment = await _paymentRepo.GetByOrderIdAsync(evt.OrderId);
        _logger.LogInformation("Retrieved payment for OrderId {OrderId}: {Payment}",
            evt.OrderId, payment is not null ? JsonSerializer.Serialize(payment, _jsonOptions) : "null");

        // GUARD CLAUSE: Stop if no payment exists
        if (payment is null)
        {
            _logger.LogWarning("No payment found for OrderId {OrderId}", evt.OrderId);
            return; // Nothing to cancel
        }

        // UPDATE STATUS: Change from Reserved to Cancelled
        payment.Status = PaymentStatus.Cancelled;
        payment.LastUpdated = DateTime.UtcNow;

        // SAVE CHANGES: Persist the status update
        await _paymentRepo.UpdateAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        // PUBLISH EVENT: Tell other services payment is cancelled
        var cancelledPayload = payment.ToCancelledPayload($"Restaurant rejected order: {evt.Reason}");
        await _paymentEventPublisher.PublishPaymentCancelledAsync(cancelledPayload, correlationId);

        _logger.LogInformation("Payment cancelled for order {OrderId} with Reason: {Reason}",
            evt.OrderId, evt.Reason);
    }

    // EVENT HANDLER 3: Delivery starts -> Finalize payment
    // This is called when RabbitMQ receives a "DeliveryStarted" event
    public async Task FinalizePaymentAsync(DeliveryStartedPayload evt, Guid correlationId)
    {
        // LOGGING: Track the delivery started event
        _logger.LogInformation("Received DeliveryStarted event for OrderId: {OrderId} with Event: {Event}",
            evt.OrderId, JsonSerializer.Serialize(evt, _jsonOptions));

        // FIND PAYMENT: Look up existing payment by OrderId
        var payment = await _paymentRepo.GetByOrderIdAsync(evt.OrderId);
        _logger.LogInformation("Retrieved payment for OrderId {OrderId}: {Payment}",
            evt.OrderId, JsonSerializer.Serialize(payment, _jsonOptions));

        // GUARD CLAUSE: Stop if no payment exists
        if (payment is null)
        {
            _logger.LogWarning("No payment found for OrderId {OrderId}", evt.OrderId);
            return; // Nothing to finalize
        }

        // UPDATE STATUS: Change from Reserved to Succeeded (money actually charged)
        payment.Status = PaymentStatus.Succeeded;
        payment.LastUpdated = DateTime.UtcNow;

        // SAVE CHANGES: Persist the final status
        await _paymentRepo.UpdateAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        // PUBLISH EVENT: Tell other services payment is complete
        var succeededPayload = payment.ToSucceededPayload();
        await _paymentEventPublisher.PublishPaymentSucceededAsync(succeededPayload, correlationId);

        _logger.LogInformation("Payment finalized for order {OrderId}", evt.OrderId);
    }
}

/*
 * SUMMARY - Payment Workflow States:
 * 
 * 1. OrderCreated → PaymentStatus.Reserved (money held, not charged)
 * 2a. RestaurantRejected → PaymentStatus.Cancelled (release the hold)
 * 2b. DeliveryStarted → PaymentStatus.Succeeded (actually charge the money)
 *
 * KEY POINTS:
 * - VALIDATION: Always validate input data before processing
 * - EARLY RETURNS: Use guard clauses to fail fast on invalid data
 * - DATABASE PATTERN: Find → Update → Save → Publish Event
 * - EVENT PUBLISHING: Always notify other services of status changes
 * - CORRELATION ID: Used for distributed tracing across services
 * - MAPPER PATTERN: ToReservedPayload(), ToCancelledPayload() extension methods
 * - STRUCTURED LOGGING: Log all important events with context
 * 
 * BUSINESS LOGIC:
 * This implements a "2-phase commit" pattern:
 * Phase 1: Reserve payment (hold money)
 * Phase 2: Either cancel (release) or succeed (charge)
 * 
 * MICROSERVICE PATTERN:
 * Each method responds to events from other services and publishes 
 * its own events, creating a choreographed workflow across services.
 */