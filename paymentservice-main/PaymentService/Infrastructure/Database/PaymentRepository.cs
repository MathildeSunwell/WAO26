using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Database.Models;

namespace PaymentService.Infrastructure.Database;

public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(Guid paymentId)
    {
        return await db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task<Guid> AddAsync(Payment payment)
    {
        var added = await db.Payments.AddAsync(payment);
        return added.Entity.Id;
    }

    public Task<Guid> UpdateAsync(Payment payment)
    {
        var updated = db.Payments.Update(payment);
        return Task.FromResult(updated.Entity.Id);
    }

    public async Task DeleteAsync(Guid paymentId)
    {
        var p = await db.Payments.FindAsync(paymentId);
        if (p != null)
            db.Payments.Remove(p);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
