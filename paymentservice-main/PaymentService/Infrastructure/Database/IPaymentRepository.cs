using PaymentService.Infrastructure.Database.Models;

namespace PaymentService.Infrastructure.Database;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId);
    Task<Guid> AddAsync(Payment payment);
    Task<Guid> UpdateAsync(Payment payment);
    Task DeleteAsync(Guid paymentId);
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
    Task SaveChangesAsync();
}
