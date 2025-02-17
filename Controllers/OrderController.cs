using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase, IDisposable
    {
        private readonly DbContexts _dbContext;
        private readonly IServiceProvider _serviceProvider;

        // Static timer and scope factory so the background update persists beyond a single controller instance.
        private static Timer _statusUpdateTimer;
        private static IServiceScopeFactory _staticScopeFactory;
        private static readonly object _timerLock = new object();

        public OrderController(DbContexts dbContext, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _serviceProvider = serviceProvider;

            // Initialize the static timer only once.
            if (_statusUpdateTimer == null)
            {
                lock (_timerLock)
                {
                    if (_statusUpdateTimer == null)
                    {
                        // Use the scope factory from the root provider.
                        _staticScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

                        // Fire immediately (TimeSpan.Zero), then every 30 seconds.
                        _statusUpdateTimer = new Timer(
                            async _ => await UpdateAllOrderStatusesStaticAsync(),
                            null,
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(30)
                        );
                    }
                }
            }
        }

        /// <summary>
        /// The static timer callback. Creates a new scope, fetches a fresh DbContext, and updates
        /// statuses for any orders that haven't reached "Completed" yet.
        /// </summary>
        private static async Task UpdateAllOrderStatusesStaticAsync()
        {
            try
            {
                // Log that the timer callback has fired using local time.
                Console.WriteLine($"[TIMER] UpdateAllOrderStatusesStaticAsync fired at {DateTime.Now:O}");

                using var scope = _staticScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DbContexts>();

                // Get all orders that are not yet Completed and have an OrderTime (which we use for transitions).
                var orders = await context.Orders
                    .Where(o => o.OrderTime != null && o.OrderStatus != "Completed")
                    .ToListAsync();

                bool updated = false;
                foreach (var order in orders)
                {
                    string prevStatus = order.OrderStatus;
                    UpdateOrderStatusStatic(order);

                    if (order.OrderStatus != prevStatus)
                    {
                        context.Entry(order).State = EntityState.Modified;
                        updated = true;
                        Console.WriteLine($"[TIMER] Order {order.Id} changed from {prevStatus} to {order.OrderStatus}.");
                    }
                }

                if (updated)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine("[TIMER] Order statuses updated in the database.");
                }
                else
                {
                    Console.WriteLine("[TIMER] No orders needed updating this cycle.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TIMER ERROR] {ex}");
            }
        }

        /// <summary>
        /// Static method that calculates the new status based on the current time vs OrderTime.
        /// The intended stages (from order creation) are:
        /// - "Preparing": until OrderTime + 30s
        /// - "Out For Delivery": from OrderTime + 30s to OrderTime + 60s
        /// - "Delivered": from OrderTime + 60s to OrderTime + 90s
        /// - "Completed": after OrderTime + 90s.
        /// </summary>
        private static void UpdateOrderStatusStatic(Order order)
        {
            // Ensure OrderTime is set.
            DateTime orderTime = order.OrderTime;
            DateTime now = DateTime.Now;
            DateTime preparingEnd = orderTime.AddSeconds(30);
            DateTime outForDeliveryEnd = orderTime.AddSeconds(60);
            DateTime deliveredEnd = orderTime.AddSeconds(90);

            if (now < preparingEnd)
            {
                order.OrderStatus = "Preparing";
            }
            else if (now < outForDeliveryEnd)
            {
                order.OrderStatus = "Out For Delivery";
            }
            else if (now < deliveredEnd)
            {
                order.OrderStatus = "Delivered";
            }
            else
            {
                order.OrderStatus = "Completed";
            }
        }

        /// <summary>
        /// Instance method that calls the same logic to keep endpoints consistent.
        /// </summary>
        private void UpdateOrderStatus(Order order)
        {
            UpdateOrderStatusStatic(order);
        }

        [HttpPost]
        public async Task<IActionResult> PostOrder([FromBody] OrderRequest orderRequest)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session ID is missing.");

            var userSession = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
                return Unauthorized("Invalid or expired session.");

            // Retrieve cart items from session.
            var cartItems = string.IsNullOrEmpty(userSession.Data)
                ? new System.Collections.Generic.List<CartItem>()
                : JsonSerializer.Deserialize<System.Collections.Generic.List<CartItem>>(
                    userSession.Data,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

            if (cartItems == null || !cartItems.Any())
                return BadRequest("Cart is empty.");

            // Build OrderItems and calculate the total.
            var orderItems = new System.Collections.Generic.List<OrderItem>();
            int subtotal = 0;
            foreach (var item in cartItems)
            {
                subtotal += item.Quantity * item.Price;
                orderItems.Add(new OrderItem
                {
                    MealKitId = item.MealKitId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            decimal shippingCost = orderRequest.ShippingCost;
            decimal tax = orderRequest.Tax;
            decimal totalAmount = subtotal + shippingCost + tax;

            // Set OrderTime to now.
            DateTime orderTime = DateTime.Now;
            // By default, set ShipTime to OrderTime + 30 seconds using local time if not provided.
            var defaultShipTime = orderTime.AddSeconds(30);

            var order = new Order
            {
                OrderType = orderRequest.OrderType,
                UserId = userSession.UserId,
                DeliveryAddress = orderRequest.DeliveryAddress,
                OrderStatus = "Preparing",
                OrderTime = orderTime, // local time
                ShipTime = orderRequest.ShipTime ?? defaultShipTime,
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Tax = tax,
                TotalAmount = totalAmount,
                OrderItems = orderItems
            };

            // Ensure correct initial status.
            UpdateOrderStatus(order);

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var orderEvent = new OrderEvent
            {
                UserId = userSession.UserId,
                OrderId = order.Id,
                TotalAmount = totalAmount,
                ItemCount = orderItems.Sum(oi => oi.Quantity)
            };
            _dbContext.OrderEvents.Add(orderEvent);

            // Clear the cart.
            userSession.Data = "[]";
            _dbContext.UserSessions.Update(userSession);
            await _dbContext.SaveChangesAsync();

            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ToListAsync();

            bool updated = false;
            foreach (var order in orders)
            {
                string prevStatus = order.OrderStatus;
                UpdateOrderStatus(order);
                if (order.OrderStatus != prevStatus)
                {
                    _dbContext.Entry(order).State = EntityState.Modified;
                    updated = true;
                }
            }
            if (updated)
                await _dbContext.SaveChangesAsync();

            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound("Order not found.");

            string prevStatus = order.OrderStatus;
            UpdateOrderStatus(order);
            if (order.OrderStatus != prevStatus)
                await _dbContext.SaveChangesAsync();

            return Ok(order);
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetOrdersByUser()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session ID is missing.");

            var userSession = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
                return Unauthorized("Invalid or expired session.");

            var orders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userSession.UserId)
                .ToListAsync();

            bool updated = false;
            foreach (var order in orders)
            {
                string prevStatus = order.OrderStatus;
                UpdateOrderStatus(order);
                if (order.OrderStatus != prevStatus)
                {
                    _dbContext.Entry(order).State = EntityState.Modified;
                    updated = true;
                }
            }
            if (updated)
                await _dbContext.SaveChangesAsync();

            return Ok(orders);
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] OrderUpdateRequest updateRequest)
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return NotFound("Order not found.");

            if (!string.IsNullOrWhiteSpace(updateRequest.OrderStatus))
                order.OrderStatus = updateRequest.OrderStatus;
            if (!string.IsNullOrWhiteSpace(updateRequest.DeliveryAddress))
                order.DeliveryAddress = updateRequest.DeliveryAddress;
            if (updateRequest.ShipTime.HasValue)
                order.ShipTime = updateRequest.ShipTime.Value;

            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();
            return Ok(order);
        }

        [HttpDelete("{orderId}")]
        public async Task<IActionResult> DeleteOrder(Guid orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return NotFound("Order not found.");

            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();
            return Ok("Order deleted successfully.");
        }

        [HttpGet("{orderId}/orderitems")]
        public async Task<IActionResult> GetOrderItemsByOrderId(Guid orderId)
        {
            var orderItems = await _dbContext.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
            if (orderItems == null || !orderItems.Any())
                return NotFound("No order items found for the given order.");

            return Ok(orderItems);
        }

        #region Request Models

        public class OrderRequest
        {
            [Required]
            public string OrderType { get; set; }
            [Required]
            public string DeliveryAddress { get; set; }
            public DateTime? ShipTime { get; set; }
            [Required]
            public decimal ShippingCost { get; set; }
            [Required]
            public decimal Tax { get; set; }
        }

        public class OrderUpdateRequest
        {
            public string OrderStatus { get; set; }
            public string DeliveryAddress { get; set; }
            public DateTime? ShipTime { get; set; }
        }

        #endregion

        public void Dispose()
        {
            // Do not dispose the static timer so it continues running across the lifetime of the application.
        }
    }
}
