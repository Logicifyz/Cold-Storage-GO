using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Services
{
    public class SubscriptionService
    {
        private readonly DbContexts _context;
        private readonly ILogger<SubscriptionService> _logger;

        // ✅ Fixed constructor issue
        public SubscriptionService(DbContexts context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ✅ Staff Freezes a Subscription
        public async Task FreezeSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
                throw new Exception("Subscription not found.");

            if (subscription.IsFrozen.GetValueOrDefault()) // ✅ Fixed null issue
                throw new Exception("Subscription is already frozen.");

            subscription.IsFrozen = true;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UnfreezeSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
                throw new Exception("Subscription not found.");

            subscription.IsFrozen = false;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task CancelSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
                throw new Exception("Subscription not found.");

            subscription.Status = "Canceled";
            subscription.EndDate = DateTime.UtcNow;
            _context.Subscriptions.Update(subscription);

            var user = await _context.Users
                .Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.UserId == subscription.UserId);

            if (user != null)
            {
                _logger.LogInformation($"✅ Subscription {subscriptionId} canceled for user {user.UserId}.");
            }

            await _context.SaveChangesAsync();
        }

        public async Task CreateSubscriptionAsync(Guid userId, int frequency, string deliveryTimeSlot, string subscriptionType, string subscriptionChoice, decimal price)
        {
            _logger.LogInformation($"🔍 Attempting to create subscription for UserId: {userId}");

            var user = await _context.Users.Include(u => u.Subscriptions).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                _logger.LogError("❌ User not found.");
                throw new Exception("User not found.");
            }

            var lastSubscription = user.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();

            if (lastSubscription != null)
            {
                _logger.LogInformation($"✅ Found previous subscription with status: {lastSubscription.Status}");

                if (lastSubscription.Status == "Active")
                {
                    _logger.LogError("❌ User already has an active subscription.");
                    throw new Exception("User already has an active subscription.");
                }
            }

            var subscription = new Subscription
            {
                SubscriptionId = Guid.NewGuid(),
                UserId = userId,
                Frequency = frequency,
                DeliveryTimeSlot = deliveryTimeSlot,
                SubscriptionType = subscriptionType,
                SubscriptionChoice = subscriptionChoice,
                Price = price,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(subscriptionType.ToLower() == "weekly" ? 7 : 30),
                AutoRenewal = false,
                IsFrozen = false,
                Status = "Active"
            };

            user.Subscriptions.Add(subscription);
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Subscription successfully created for user {userId}.");
        }

        public async Task<bool> UserHasSubscriptionAsync(Guid userId)
        {
            return await _context.Subscriptions
                .AnyAsync(s => s.UserId == userId && s.Status == "Active");
        }

        public async Task<Subscription?> GetActiveSubscriptionByUserIdAsync(Guid userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId && s.Status == "Active")
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionHistoryAsync(Guid userId)
        {
            return await _context.Subscriptions
                .Where(s => s.UserId == userId && (s.Status == "Canceled" || s.Status == "Expired"))
                .OrderByDescending(s => s.EndDate)
                .ToListAsync();
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

        public async Task ExtendOrExpireSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");

            if (subscription.EndDate <= DateTime.UtcNow)
            {
                if (subscription.AutoRenewal.GetValueOrDefault()) // ✅ Fixed nullable bool issue
                {
                    DateTime nextStartDate = subscription.EndDate.AddDays(1).Date;
                    int duration = subscription.SubscriptionType.ToLower() == "weekly" ? 7 : 30;
                    DateTime nextEndDate = nextStartDate.AddDays(duration).AddSeconds(-1);

                    subscription.StartDate = nextStartDate;
                    subscription.EndDate = nextEndDate;
                    subscription.Status = "Active";
                }
                else
                {
                    subscription.Status = "Expired";
                }
            }

            _context.Subscriptions.Update(subscription);
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
                return await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error fetching subscription by user ID: {ex.Message}");
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

            subscription.AutoRenewal = !subscription.AutoRenewal;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        // ✅ Fixed: Re-added missing methods referenced in SubscriptionsController
        public async Task<IEnumerable<Subscription>> GetSubscriptionsByChoiceAsync(string subscriptionChoice)
        {
            return await _context.Subscriptions.Where(s => s.SubscriptionChoice == subscriptionChoice).ToListAsync();
        }

        public async Task<IEnumerable<Subscription>> GetAllSubscriptionsAsync()
        {
            return await _context.Subscriptions.ToListAsync();
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionsByStatusAsync(bool isFrozen)
        {
            return await _context.Subscriptions.Where(s => s.IsFrozen == isFrozen).ToListAsync();
        }

        public async Task<IEnumerable<Subscription>> SearchSubscriptionsAsync(string query)
        {
            return await _context.Subscriptions.Where(s => s.UserId.ToString().Contains(query)).ToListAsync();
        }
    }
}
