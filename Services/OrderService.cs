using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services;

public class OrderService
{
    private readonly DbContexts _context;

    public OrderService(DbContexts context)
    {
        _context = context;
    }

    public async Task CreateOrderAsync(Guid subscriptionId, Guid userId, Guid mealKitId, int totalPrice, string promotionCode, string orderNotes)
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            UserId = userId,
            MealKitId = mealKitId,
            OrderDate = DateTime.UtcNow,
            TotalPrice = totalPrice,
            PromotionCode = promotionCode,
            OrderNotes = orderNotes
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersBySubscriptionAsync(Guid subscriptionId)
    {
        return await _context.Orders
            .Where(o => o.SubscriptionId == subscriptionId)
            .ToListAsync();
    }
}
