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
            if (subscription == null)
            {
                throw new Exception("Subscription not found.");
            }

            if ((bool)subscription.IsFrozen)
            {
                throw new Exception("Subscription is already frozen.");
            }

            subscription.IsFrozen = true;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UnfreezeSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
            {
                throw new Exception("Subscription not found.");
            }

            subscription.IsFrozen = false;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }


        public async Task CancelSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");

            _context.Subscriptions.Remove(subscription);

            // Refund points for all remaining days
            int daysRemaining = (subscription.EndDate - DateTime.UtcNow.Date).Days;
            int pointsToRefund = daysRemaining * 10;
            Console.WriteLine($"User refunded {pointsToRefund} points.");

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
        public async Task CreateSubscriptionAsync(Guid userId, Guid mealKitId, int frequency, string deliveryTimeSlot, string subscriptionType, string subscriptionChoice, decimal price)
        {
            var subscription = new Subscription
            {
                SubscriptionId = Guid.NewGuid(),
                UserId = userId,
                MealKitId = mealKitId,
                Frequency = frequency,
                DeliveryTimeSlot = deliveryTimeSlot,
                SubscriptionType = subscriptionType,
                SubscriptionChoice = subscriptionChoice,
                Price = price,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(frequency * 7),

                // ✅ Set default values for nullable fields during creation
                AutoRenewal = false,
                IsFrozen = false,
                StripeSessionId = Guid.NewGuid().ToString()
            };

            try
            {
                _context.Subscriptions.Add(subscription);
                var result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    throw new Exception("Database write failed: No rows affected.");
                }

                Console.WriteLine("✅ Subscription successfully saved to the database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database Error: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateSubscriptionStatusAsync(Guid subscriptionId, UpdateSubscriptionRequest request)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");

            if (request.AutoRenewal.HasValue)
                subscription.AutoRenewal = request.AutoRenewal.Value;

            if (request.IsFrozen.HasValue)
                subscription.IsFrozen = request.IsFrozen.Value;

            await _context.SaveChangesAsync();
        }

        public async Task ExtendSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");

            if ((bool)!subscription.AutoRenewal) return; // Only renew if auto-renewal is enabled

            // Calculate next start and end date based on subscription type
            DateTime nextStartDate = subscription.EndDate.AddDays(1).Date; // Start next day at midnight
            int duration = subscription.SubscriptionType.ToLower() == "weekly" ? 7 : 30;
            DateTime nextEndDate = nextStartDate.AddDays(duration).AddSeconds(-1);

            subscription.StartDate = nextStartDate;
            subscription.EndDate = nextEndDate;

            await _context.SaveChangesAsync();
        }
        public async Task<Subscription> GetSubscriptionByIdAsync(Guid subscriptionId)
        {
            return await _context.Subscriptions.FindAsync(subscriptionId);
        }

        public async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
        public async Task<Subscription?> GetSubscriptionByUserIdAsync(Guid userId)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                return subscription;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching subscription by user ID: {ex.Message}");
                return null;
            }
        }
        public async Task ToggleAutoRenewalAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
            {
                throw new Exception("Subscription not found.");
            }

            subscription.AutoRenewal = !subscription.AutoRenewal; // Toggle the value
            _context.Subscriptions.Update(subscription);  // Ensure it tracks the entity
            await _context.SaveChangesAsync();  // Persist the change
        }


    }
}
