using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OrderTrackingService.Domain.DTOs;
using OrderTrackingService.Domain.Enums;
using OrderTrackingService.Infrastructure.Database.Models;

namespace OrderTrackingService.Infrastructure.Database;

public class OrderRepository(ApplicationDbContext db, ILogger<OrderRepository> logger) : IOrderRepository
{
    public async Task<Order?> GetByOrderIdAsync(Guid orderId)
    {
        IQueryable<Order> q = db.Orders;
        return await q.SingleOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<IEnumerable<Order>> GetByFilterAsync(OrderQueryParameters filters)
    {
        IQueryable<Order> q = db.Orders;

        if (!string.IsNullOrWhiteSpace(filters.OrderStatus))
            q = q.Where(o => o.OrderStatus == filters.OrderStatus);
        
        if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
            q = q.Where(o => o.PaymentStatus == filters.PaymentStatus);

        if (!string.IsNullOrWhiteSpace(filters.RestaurantStatus))
            q = q.Where(o => o.RestaurantStatus == filters.RestaurantStatus);

        if (!string.IsNullOrWhiteSpace(filters.DeliveryStatus))
            q = q.Where(o => o.DeliveryStatus == filters.DeliveryStatus);

        if (filters.CreatedAfter.HasValue)
            q = q.Where(o => o.CreateTime >= filters.CreatedAfter.Value);

        if (filters.CreatedBefore.HasValue)
            q = q.Where(o => o.CreateTime <= filters.CreatedBefore.Value);

        var skip = (filters.Page - 1) * filters.PageSize;
        return await q
            .OrderByDescending(o => o.CreateTime)
            .Skip(skip)
            .Take(filters.PageSize)
            .ToListAsync();
    }

    public async Task<Guid> AddAsync(Order order)
    {
        var addedOrder = await db.Orders.AddAsync(order);
        return addedOrder.Entity.Id;
    }

    public Task<Guid> UpdateAsync(Order order)
    {
        var updatedOrder = db.Orders.Update(order);
        return Task.FromResult(updatedOrder.Entity.Id);
    }

    public async Task DeleteAsync(Guid orderId)
    {
        var o = await db.Orders.FindAsync(orderId);
        if (o != null) db.Orders.Remove(o);
    }

    public async Task<string?> SaveChangesWithChangeCheckAsync()
    {
        string? error = null;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.Single();
            error = HandleOrderConcurrency(db, entry);
            if (error == null)
            {
                logger.LogInformation("Handled concurrency conflict, saving changes again");
                await db.SaveChangesAsync();
            }
            else 
            {
                logger.LogWarning("Failed to resolve concurrency conflict: {ErrorMessage}", error);
            }
        }
        return error;
    }
    
    private static string? HandleOrderConcurrency(DbContext context, EntityEntry entry)
    {
        if (entry.Entity is not Order attempted)
            throw new NotSupportedException(
                $"Don't know how to handle concurrency conflicts for {entry.Metadata.Name}");

        // as it exists in the database now
        var databaseOrder = context.Set<Order>()
            .AsNoTracking()
            .SingleOrDefault(o => o.OrderId == attempted.OrderId);

        if (databaseOrder == null)
            return "Unable to save changes to order. It has been deleted.";

        // EF tracking info
        var databaseEntry = context.Entry(databaseOrder);
        
        var statusProps = new[]
        {
            nameof(Order.OrderStatus),
            nameof(Order.PaymentStatus),
            nameof(Order.RestaurantStatus),
            nameof(Order.DeliveryStatus)
        };

        foreach (var prop in entry.Metadata.GetProperties())
        {
            var name = prop.Name;

            var currentString  = entry.Property(name).CurrentValue  as string;
            var databaseString = databaseEntry.Property(name).CurrentValue as string;

            if (statusProps.Contains(name))
            {
                var (enumType, priority) = name switch
                {
                    nameof(Order.OrderStatus)      => (
                        typeof(OrderStatus),
                        new[] { "Cancelled", "Completed", "Progressing", "Pending" }
                    ),
                    nameof(Order.PaymentStatus)    => (
                        typeof(PaymentStatus),
                        new[] { "Cancelled", "Failed", "Succeeded", "Reserved", "Pending" }
                    ),
                    nameof(Order.RestaurantStatus) => (
                        typeof(RestaurantStatus),
                        new[] { "Cancelled", "Rejected", "Completed", "Ready", "Accepted", "Pending" }
                    ),
                    nameof(Order.DeliveryStatus)   => (
                        typeof(DeliveryStatus),
                        new[] { "Cancelled", "Completed", "Started", "Assigned", "Pending" }
                    ),
                    _ => throw new InvalidOperationException($"Unknown status prop '{name}'")
                };

                var triedEnum = (Enum)Enum.Parse(enumType, currentString!);
                var dbEnum = (Enum)Enum.Parse(enumType, databaseString!);

                var chosenName = priority
                    .FirstOrDefault(p =>
                        triedEnum.ToString() == p || dbEnum.ToString() == p
                    )
                    ?? triedEnum.ToString();

                entry.Property(name).CurrentValue  = chosenName;
                entry.Property(name).OriginalValue = databaseString;
            }
            else if (prop.Name == nameof(Order.LastUpdated))
            {
                // use later timestamp
                var tried = (DateTime)entry.Property(prop.Name).CurrentValue!;
                var db = (DateTime)databaseEntry.Property(prop.Name).CurrentValue!;
                entry.Property(prop.Name).CurrentValue = tried > db ? tried : db;
            }
            
            entry.Property(prop.Name).OriginalValue = databaseEntry.Property(prop.Name).CurrentValue;
        }
        
        return entry.Properties.Any(p => p.Metadata.Name == nameof(Order.LastUpdated) || statusProps.Contains(p.Metadata.Name)) 
            ? null 
            : "Unable to resolve concurrency conflict automatically";
    }

}

public interface IOrderRepository
{
    Task<Order?> GetByOrderIdAsync(Guid orderId);
    Task<IEnumerable<Order>> GetByFilterAsync(OrderQueryParameters filters);
    Task<Guid> AddAsync(Order order);
    Task<Guid> UpdateAsync(Order order);
    Task DeleteAsync(Guid orderId);
    Task<string?> SaveChangesWithChangeCheckAsync();
}