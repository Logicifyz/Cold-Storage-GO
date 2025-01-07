using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services;

public class SubscriptionService
{
    private readonly DbContexts _context;

    public SubscriptionService(DbContexts context)
    {
        _context = context;
    }

    public async Task CreateSubscriptionAsync(Guid userId, Guid mealKitId, int frequency, bool autoRenewal)
    {
        var subscription = new Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            UserId = userId,
            MealKitId = mealKitId,
            Frequency = frequency,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(frequency),
            AutoRenewal = autoRenewal
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(Guid userId)
    {
        return await _context.Subscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }
}
