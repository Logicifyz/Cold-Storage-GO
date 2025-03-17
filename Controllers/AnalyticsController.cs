using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly DbContexts _context;

        public AnalyticsController(DbContexts context)
        {
            _context = context;
        }

        [HttpGet("cart-activity")]
        public async Task<IActionResult> GetCartActivity([FromQuery] string timeframe = "7d")
        {
            var cutoffDate = GetCutoffDate(timeframe);

            var data = await _context.CartEvents
                .Where(e => e.EventTime >= cutoffDate)
                .GroupBy(e => e.EventTime.Date)
                .Select(g => new {
                    date = g.Key,
                    AddToCartCount = g.Count(),
                    TotalQuantity = g.Sum(e => e.Quantity)
                })
                .OrderBy(d => d.date)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("order-activity")]
        public async Task<IActionResult> GetOrderActivity([FromQuery] string timeframe = "7d")
        {
            var cutoffDate = GetCutoffDate(timeframe);

            var data = await _context.OrderEvents
                .Where(e => e.EventTime >= cutoffDate)
                .GroupBy(e => e.EventTime.Date)
                .Select(g => new {
                    date = g.Key,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(e => e.TotalAmount),
                    AverageOrderValue = g.Average(e => e.TotalAmount),
                    TotalItemsSold = g.Sum(e => e.ItemCount)
                })
                .OrderBy(d => d.date)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("support-activity")]
        public async Task<IActionResult> GetSupportActivity([FromQuery] string timeframe = "7d")
        {
            var cutoffDate = GetCutoffDate(timeframe);

            var data = await _context.SupportTicketEvents
                .Where(e => e.CreatedAt >= cutoffDate)
                .GroupBy(e => e.CreatedAt.Date)
                .Select(g => new {
                    date = g.Key,
                    SupportTicketEventCount = g.Count(),
                    ResolvedTickets = g.Count(e => e.Status == "Resolved")
                })
                .OrderBy(d => d.date)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("subscription-activity")]
        public async Task<IActionResult> GetSubscriptionActivity([FromQuery] string timeframe = "7d")
        {
            var cutoffDate = GetCutoffDate(timeframe);

            var data = await _context.SubscriptionEvents
                .Where(e => e.EventTime >= cutoffDate)
                .GroupBy(e => e.EventTime.Date)
                .Select(g => new {
                    date = g.Key,
                    SubscriptionEventCount = g.Count(),
                    Cancellations = g.Count(e => e.EventType == "SubscriptionCanceled"),
                    Renewals = g.Count(e => e.EventType == "Renewed")
                })
                .OrderBy(d => d.date)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("reward-redemption-activity")]
        public async Task<IActionResult> GetRewardRedemptionActivity([FromQuery] string timeframe = "7d")
        {
            var cutoffDate = GetCutoffDate(timeframe);

            var data = await _context.RewardRedemptionEvents
                .Where(e => e.RedeemedAt >= cutoffDate)
                .GroupBy(e => e.RedeemedAt.Date)
                .Select(g => new {
                    date = g.Key,
                    RedemptionEventCount = g.Count()
                })
                .OrderBy(d => d.date)
                .ToListAsync();

            return Ok(data);
        }

        private DateTime GetCutoffDate(string timeframe)
        {
            return timeframe switch
            {
                "24h" => DateTime.UtcNow.AddHours(-24),
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                _ => DateTime.UtcNow.AddDays(-7)
            };
        }
    }
}
