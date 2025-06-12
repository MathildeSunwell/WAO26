using PaymentService.Domain.Events;

namespace PaymentService.Infrastructure.Messaging;

public interface IPaymentEventPublisher
{
    Task PublishPaymentReservedAsync(PaymentReservedPayload paymentEvent, Guid correlationId);
    Task PublishPaymentFailedAsync(PaymentFailedPayload paymentEvent, Guid correlationId);
    Task PublishPaymentSucceededAsync(PaymentSucceededPayload paymentEvent, Guid correlationId);
    Task PublishPaymentCancelledAsync(PaymentCancelledPayload paymentEvent, Guid correlationId);
}