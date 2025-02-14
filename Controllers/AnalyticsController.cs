using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                        Date = g.Key,
                        AddToCartCount = g.Count(),
                        TotalQuantity = g.Sum(e => e.Quantity)
                    })
                    .OrderBy(d => d.Date)
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
                        Date = g.Key,
                        OrderCount = g.Count(),
                        TotalRevenue = g.Sum(e => e.TotalAmount),
                        AverageOrderValue = g.Average(e => e.TotalAmount),
                        TotalItemsSold = g.Sum(e => e.ItemCount)
                    })
                    .OrderBy(d => d.Date)
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

