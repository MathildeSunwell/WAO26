using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Database.Models;

namespace PaymentService.Infrastructure.Database;

// REPOSITORY PATTERN: This class encapsulates all database operations for Payment entities
public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    // QUERY METHOD 1: Find a payment by its unique ID
    // SingleOrDefaultAsync = Expects 0 or 1 result, throws if multiple found
    public async Task<Payment?> GetByIdAsync(Guid paymentId)
    {
        // Translates to: SELECT * FROM Payments WHERE Id = @paymentId
        return await db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId);
    }

    // CREATE METHOD: Add a new payment to the database
    // Note: This only stages the change - doesn't hit database until SaveChangesAsync()
    public async Task<Guid> AddAsync(Payment payment)
    {
        // AddAsync stages the entity for insertion
        var added = await db.Payments.AddAsync(payment);
        
        return added.Entity.Id;
    }

    // UPDATE METHOD: Mark an existing payment as modified
    // Note: This is NOT async because Update() only marks the entity as "Modified"
    public Task<Guid> UpdateAsync(Payment payment)
    {
        // Update() tells EF this entity has been modified
        // EF will generate UPDATE SQL when SaveChangesAsync() is called
        var updated = db.Payments.Update(payment);
        
        return Task.FromResult(updated.Entity.Id);
    }

    // DELETE METHOD: Remove a payment from the database
    // Two-step process: Find the entity, then mark for deletion
    public async Task DeleteAsync(Guid paymentId)
    {
        // STEP 1: Find the entity to delete
        var p = await db.Payments.FindAsync(paymentId);
        
        // STEP 2: Mark for deletion (if found)
        // Guard clause: only delete if entity exists
        if (p != null)
            db.Payments.Remove(p);  // Marks as "Deleted" state
        
        // NOTE: Actual deletion happens in SaveChangesAsync()
    }

    // QUERY METHOD 2: Find payment by business key (OrderId)
    // FirstOrDefaultAsync = Gets first match or null, doesn't throw if multiple
    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        // Translates to: SELECT TOP(1) * FROM Payments WHERE OrderId = @orderId
        return await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    // PERSISTENCE METHOD: Commit all changes to the database
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
