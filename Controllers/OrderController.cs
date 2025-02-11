using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers

{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly DbContexts _dbContext;

        public OrderController(DbContexts dbContext)
        {
            _dbContext = dbContext;
        }

        // POST: api/order
        // Creates an order based on the current cart (referencing CartController logic)
        [HttpPost]
        public async Task<IActionResult> PostOrder([FromBody] OrderRequest orderRequest)
        {
            // Retrieve session ID and validate session
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is missing.");
            }

            var userSession = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
            {
                return Unauthorized("Invalid or expired session.");
            }

            // Retrieve cart items (using case-insensitive deserialization)
            var cartItems = string.IsNullOrEmpty(userSession.Data)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(
                      userSession.Data,
                      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                  );

            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest("Cart is empty.");
            }

            // Create order items from cart data and calculate subtotal
            List<OrderItem> orderItems = new List<OrderItem>();
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

            // Calculate total amount
            decimal shippingCost = orderRequest.ShippingCost;
            decimal tax = orderRequest.Tax;
            decimal totalAmount = subtotal + shippingCost + tax;

            // Create the order
            Order order = new Order
            {
                OrderType = orderRequest.OrderType,
                UserId = userSession.UserId, // Assuming UserSession contains UserId
                DeliveryAddress = orderRequest.DeliveryAddress,
                OrderStatus = orderRequest.OrderStatus,
                OrderTime = DateTime.UtcNow,
                ShipTime = orderRequest.ShipTime,
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Tax = tax,
                TotalAmount = totalAmount,
                OrderItems = orderItems
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            // Clear the cart after order placement
            userSession.Data = "[]";
            _dbContext.UserSessions.Update(userSession);
            await _dbContext.SaveChangesAsync();

            return Ok(order);
        }

        // GET: api/order
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _dbContext.Orders.Include(o => o.OrderItems).ToListAsync();
            return Ok(orders);
        }

        // GET: api/order/{orderId}
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound("Order not found.");
            }
            return Ok(order);
        }

        // GET: api/order/user
        // Retrieves orders for the current user using the session cookie
        [HttpGet("user")]
        public async Task<IActionResult> GetOrdersByUser()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is missing.");
            }

            var userSession = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
            {
                return Unauthorized("Invalid or expired session.");
            }

            var orders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userSession.UserId)
                .ToListAsync();
            return Ok(orders);
        }

        // PUT: api/order/{orderId}
        [HttpPut("{orderId}")]
        public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] OrderUpdateRequest updateRequest)
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Update fields if provided
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

        // DELETE: api/order/{orderId}
        [HttpDelete("{orderId}")]
        public async Task<IActionResult> DeleteOrder(Guid orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound("Order not found.");
            }
            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();
            return Ok("Order deleted successfully.");
        }

        // GET: api/order/{orderId}/orderitems
        [HttpGet("{orderId}/orderitems")]
        public async Task<IActionResult> GetOrderItemsByOrderId(Guid orderId)
        {
            var orderItems = await _dbContext.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
            if (orderItems == null || !orderItems.Any())
            {
                return NotFound("No order items found for the given order.");
            }
            return Ok(orderItems);
        }

        #region Request Models

        public class OrderRequest
        {
            [Required]
            public string OrderType { get; set; }

            [Required]
            public string DeliveryAddress { get; set; }

            [Required]
            public string OrderStatus { get; set; }

            // Optional ship time if provided by the client
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
    }
}
