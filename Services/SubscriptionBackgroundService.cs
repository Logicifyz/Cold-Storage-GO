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

                    // ✅ Find subscriptions scheduled for freezing today
                    var scheduledFreezes = await dbContext.SubscriptionFreezeHistories
                        .Where(f => f.FreezeStartDate == DateTime.UtcNow.Date && f.FreezeEndDate == null)
                        .ToListAsync();

                    foreach (var freeze in scheduledFreezes)
                    {
                        await subscriptionService.FreezeSubscriptionAsync(freeze.SubscriptionId);
                    }

                    // ✅ Process expired subscriptions
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
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

    }
}
