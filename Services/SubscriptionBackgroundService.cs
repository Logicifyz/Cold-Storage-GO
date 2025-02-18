using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services
{
    public class SubscriptionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionBackgroundService> _logger;

        public SubscriptionBackgroundService(IServiceProvider serviceProvider, ILogger<SubscriptionBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ SubscriptionBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var subscriptionService = scope.ServiceProvider.GetRequiredService<SubscriptionService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<DbContexts>();

                    // ✅ Activate scheduled freezes
                    var freezesToActivate = await dbContext.SubscriptionFreezeHistories
                        .Where(f => f.FreezeStartDate <= DateTime.UtcNow && f.FreezeEndDate == null)
                        .ToListAsync();

                    foreach (var freeze in freezesToActivate)
                    {
                        try
                        {
                            await subscriptionService.FreezeSubscriptionAsync(freeze.SubscriptionId);
                            _logger.LogInformation($"✅ Activated freeze for Subscription ID: {freeze.SubscriptionId}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"❌ Failed to activate freeze for Subscription ID: {freeze.SubscriptionId}. Error: {ex.Message}");
                        }
                    }

                    // ✅ Deactivate expired freezes
                    var freezesToDeactivate = await dbContext.SubscriptionFreezeHistories
                        .Where(f => f.FreezeEndDate <= DateTime.UtcNow && f.FreezeEndDate != null)
                        .ToListAsync();

                    foreach (var freeze in freezesToDeactivate)
                    {
                        try
                        {
                            await subscriptionService.UnfreezeSubscriptionAsync(freeze.SubscriptionId);
                            _logger.LogInformation($"✅ Deactivated freeze for Subscription ID: {freeze.SubscriptionId}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"❌ Failed to deactivate freeze for Subscription ID: {freeze.SubscriptionId}. Error: {ex.Message}");
                        }
                    }

                    // ✅ Process expired subscriptions (existing logic)
                    var expiredSubscriptions = await dbContext.Subscriptions
                        .Where(s => s.EndDate <= DateTime.UtcNow && s.Status == "Active")
                        .ToListAsync();

                    foreach (var sub in expiredSubscriptions)
                    {
                        _logger.LogInformation($"🔄 Checking expired subscription: {sub.SubscriptionId}");

                        try
                        {
                            await subscriptionService.ExtendOrExpireSubscriptionAsync(sub.SubscriptionId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"⚠️ Failed to process expired subscription {sub.SubscriptionId}: {ex.Message}");
                        }
                    }
                }

                _logger.LogInformation("Check complete.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); // Check every minute
            }
        }

    }
}
