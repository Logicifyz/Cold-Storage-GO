using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
        {
            await _orderService.CreateOrderAsync(request.UserId, request.MealKitId, request.TotalPrice);
            return Ok("Order created successfully");
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] string status)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, status);
            return Ok($"Order status updated to {status}");
        }

        // New: Staff GET for all orders
        [HttpGet("staff/all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("staff/status/{status}")]
        public async Task<IActionResult> GetOrdersByStatus(string status)
        {
            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }

        [HttpGet("staff/search")]
        public async Task<IActionResult> SearchOrders([FromQuery] string query)
        {
            var orders = await _orderService.SearchOrdersAsync(query);
            return Ok(orders);
        }
    }
}
