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
            if (subscription == null) throw new Exception("Subscription not found.");

            // ✅ Prevent immediate freezing if a future freeze is already scheduled
            var scheduledFreeze = await _context.SubscriptionFreezeHistories
                .Where(f => f.SubscriptionId == subscriptionId && f.FreezeStartDate > DateTime.UtcNow.Date)
                .FirstOrDefaultAsync();

            if (scheduledFreeze != null)
            {
                return; // ✅ Skip instead of throwing an error
            }

            if (subscription.IsFrozen ?? false)
            {
                return; // ✅ Exit instead of 
            }

            var freezeRecord = new SubscriptionFreezeHistory
            {
                SubscriptionId = subscriptionId,
                FreezeStartDate = DateTime.UtcNow.Date,
    FreezeEndDate = DateTime.UtcNow.Date.AddDays(7) // Example: Freeze for 7 days
            };

            _context.SubscriptionFreezeHistories.Add(freezeRecord);
            subscription.IsFrozen = true;

            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        // ✅ Unfreeze a subscription immediately
        public async Task UnfreezeSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");

            if (!subscription.IsFrozen ?? false)
            {
                throw new Exception("Subscription is not frozen.");
            }

            var freezeRecord = await _context.SubscriptionFreezeHistories
                .Where(f => f.SubscriptionId == subscriptionId && f.FreezeEndDate == null)
                .OrderByDescending(f => f.FreezeStartDate)
                .FirstOrDefaultAsync();

            if (freezeRecord != null)
            {
                freezeRecord.FreezeEndDate = DateTime.UtcNow.Date;
                _context.SubscriptionFreezeHistories.Update(freezeRecord);
            }

            subscription.IsFrozen = false;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task CancelSubscriptionAsync(Guid subscriptionId)
        {
            _logger.LogInformation($"🟢 CancelSubscriptionAsync called for Subscription ID: {subscriptionId}");

            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
            {
                _logger.LogError($"❌ Subscription not found. ID: {subscriptionId}");
                throw new Exception("Subscription not found.");
            }

            var user = await _context.Users.FindAsync(subscription.UserId);
            if (user == null)
            {
                _logger.LogError($"❌ User not found for Subscription ID: {subscriptionId}");
                throw new Exception("User not found.");
            }

            _logger.LogInformation($"🔍 Subscription found. User ID: {user.UserId}, Subscription Type: {subscription.SubscriptionType}");

            // Ensure cancellation starts from the next day
            DateTime cancelEffectiveDate = DateTime.UtcNow.Date.AddDays(1);
            int remainingDays = (subscription.EndDate - cancelEffectiveDate).Days;

            if (remainingDays > 0)
            {
                int refundPoints = remainingDays * (int)subscription.Price / (subscription.SubscriptionType.ToLower() == "weekly" ? 7 : 30);

                _logger.LogInformation($"💰 Calculated Refund: {refundPoints} points for User {user.UserId}");

                // ✅ Call Wallet API to add points
                using var httpClient = new HttpClient();

                var earnPointsRequest = JsonContent.Create(new
                {
                    UserId = user.UserId,
                    Coins = refundPoints
                });

                _logger.LogInformation($"🔗 Sending refund request to Wallet API for {refundPoints} points");

                var response = await httpClient.PostAsync("http://localhost:5135/api/Wallet/earn", earnPointsRequest);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ {refundPoints} points refunded to User {user.UserId}.");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to add refund points. Response: {response.StatusCode} | {responseContent}");
                }
            }
            else
            {
                _logger.LogWarning($"⚠️ No points refunded. Subscription too close to expiry for User {user.UserId}.");
            }

            subscription.Status = "Canceled";
            subscription.EndDate = cancelEffectiveDate;

            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Subscription {subscriptionId} successfully canceled.");
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
                // ✅ Step 1: Expire the subscription first
                subscription.Status = "Expired";
                _context.Subscriptions.Update(subscription);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"⏳ Subscription {subscription.SubscriptionId} expired for User {subscription.UserId}");

                // ✅ Step 2: Check if Auto-Renewal is enabled & prevent duplicates
                if (subscription.AutoRenewal ?? false)
                {
                    bool hasActiveSubscription = await _context.Subscriptions
                        .AnyAsync(s => s.UserId == subscription.UserId && s.Status == "Active");

                    if (hasActiveSubscription)
                    {
                        _logger.LogWarning($"⚠️ User {subscription.UserId} already has an active subscription. Skipping auto-renewal.");
                        return; // ✅ Prevents multiple active subscriptions
                    }

                    // ✅ Auto-Renewal Enabled - Create a New Subscription
                    DateTime nextStartDate = subscription.EndDate.AddDays(1).Date;
                    int duration = subscription.SubscriptionType.ToLower() == "weekly" ? 7 : 30;
                    DateTime nextEndDate = nextStartDate.AddDays(duration).AddSeconds(-1);

                    var newSubscription = new Subscription
                    {
                        SubscriptionId = Guid.NewGuid(),
                        UserId = subscription.UserId,
                        Frequency = subscription.Frequency,
                        DeliveryTimeSlot = subscription.DeliveryTimeSlot,
                        SubscriptionType = subscription.SubscriptionType,
                        SubscriptionChoice = subscription.SubscriptionChoice,
                        Price = subscription.Price,
                        StartDate = nextStartDate,
                        EndDate = nextEndDate,
                        AutoRenewal = true, // ✅ Keep Auto-Renewal On
                        IsFrozen = false,
                        Status = "Active"
                    };

                    _context.Subscriptions.Add(newSubscription);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"🔄 Auto-Renewal: Created new subscription {newSubscription.SubscriptionId} for User {subscription.UserId}");
                }
                else
                {
                    // ✅ Step 3: Process Freeze Refund if No Auto-Renewal
                    var freezeRecords = await _context.SubscriptionFreezeHistories
                        .Where(f => f.SubscriptionId == subscriptionId && f.FreezeEndDate != null)
                        .ToListAsync();

                    int totalFreezeDays = freezeRecords.Sum(f => (f.FreezeEndDate.Value - f.FreezeStartDate).Days);

                    if (totalFreezeDays > 0)
                    {
                        int refundPoints = totalFreezeDays * (int)subscription.Price / (subscription.SubscriptionType.ToLower() == "weekly" ? 7 : 30);

                        _logger.LogInformation($"✅ Refunding {refundPoints} points for {totalFreezeDays} freeze days.");

                        using (var httpClient = new HttpClient())
                        {
                            var earnPointsRequest = new
                            {
                                UserId = subscription.UserId,
                                Coins = refundPoints
                            };

                            var response = await httpClient.PostAsJsonAsync("http://localhost:5135/api/Wallet/earn", earnPointsRequest);

                            if (response.IsSuccessStatusCode)
                            {
                                _logger.LogInformation($"✅ {refundPoints} points refunded to User {subscription.UserId}.");
                            }
                            else
                            {
                                _logger.LogError($"❌ Failed to refund freeze points. Response: {await response.Content.ReadAsStringAsync()}");
                            }
                        }
                    }
                }
            }
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

            subscription.AutoRenewal = !(subscription.AutoRenewal ?? false);
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

        // ✅ Schedule a freeze for the future
        public async Task ScheduleFreezeAsync(Guid subscriptionId, DateTime startDate)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) throw new Exception("Subscription not found.");
            if (subscription.Status != "Active") throw new Exception("Only active subscriptions can be scheduled for freezing.");
            if (startDate < DateTime.UtcNow.Date) throw new Exception("Freeze start date must be in the future.");

            var existingFreeze = await _context.SubscriptionFreezeHistories
                .Where(f => f.SubscriptionId == subscriptionId && f.FreezeStartDate == startDate)
                .FirstOrDefaultAsync();

            if (existingFreeze != null)
            {
                throw new Exception($"A freeze is already scheduled for {startDate:yyyy-MM-dd}.");
            }

            var freezeRecord = new SubscriptionFreezeHistory
            {
                SubscriptionId = subscriptionId,
                FreezeStartDate = startDate, // ✅ Future freeze date
                FreezeEndDate = null // Will be set when unfrozen
            };

            _context.SubscriptionFreezeHistories.Add(freezeRecord);
            await _context.SaveChangesAsync();
        }

        // ✅ Cancel a scheduled freeze before it happens
        public async Task CancelScheduledFreezeAsync(Guid subscriptionId)
        {
            var freezeRecord = await _context.SubscriptionFreezeHistories
                .Where(f => f.SubscriptionId == subscriptionId && f.FreezeStartDate > DateTime.UtcNow.Date)
                .FirstOrDefaultAsync();

            if (freezeRecord == null)
            {
                throw new Exception("No upcoming freeze found to cancel.");
            }

            _context.SubscriptionFreezeHistories.Remove(freezeRecord);
            await _context.SaveChangesAsync();
        }


    }
}
