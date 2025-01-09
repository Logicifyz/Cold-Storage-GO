using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services
{
    public class SubscriptionService
    {
        private readonly DbContexts _context;

        public SubscriptionService(DbContexts context)
        {
            _context = context;
        }

        // ✅ Updated: Now includes SubscriptionChoice
        public async Task CreateSubscriptionAsync(Guid userId, Guid mealKitId, int frequency, string deliveryTimeSlot, string subscriptionType, string subscriptionChoice)
        {
            var subscription = new Subscription
            {
                SubscriptionId = Guid.NewGuid(),
                UserId = userId,
                MealKitId = mealKitId,
                Frequency = frequency,
                DeliveryTimeSlot = deliveryTimeSlot,
                SubscriptionType = subscriptionType,
                SubscriptionChoice = subscriptionChoice,  // New field added
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(frequency * 7)
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        // ✅ Staff Freezes a Subscription
        public async Task FreezeSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");

            subscription.IsFrozen = true;
            await _context.SaveChangesAsync();
        }

        // ✅ Staff Cancels a Subscription
        public async Task CancelSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
        }

        // ✅ New: Get Subscriptions by SubscriptionChoice
        public async Task<IEnumerable<Subscription>> GetSubscriptionsByChoiceAsync(string subscriptionChoice)
        {
            return await _context.Subscriptions
                .Where(s => s.SubscriptionChoice == subscriptionChoice)
                .ToListAsync();
        }

        // ✅ Staff Management: Get All Subscriptions
        public async Task<IEnumerable<Subscription>> GetAllSubscriptionsAsync()
        {
            return await _context.Subscriptions.ToListAsync();
        }

        // ✅ Staff Management: Get Subscriptions by Status
        public async Task<IEnumerable<Subscription>> GetSubscriptionsByStatusAsync(bool isFrozen)
        {
            return await _context.Subscriptions
                .Where(s => s.IsFrozen == isFrozen)
                .ToListAsync();
        }

        // ✅ Staff Management: Search Subscriptions by User ID
        public async Task<IEnumerable<Subscription>> SearchSubscriptionsAsync(string query)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId.ToString().Contains(query))
                .ToListAsync();
        }
    }
}
