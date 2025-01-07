using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _service;

        public OrdersController(OrderService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            await _service.CreateOrderAsync(request.SubscriptionId, request.UserId, request.MealKitId, request.TotalPrice, request.PromotionCode, request.OrderNotes);
            return Ok("Order created successfully!");
        }

        [HttpGet("{subscriptionId}")]
        public async Task<IActionResult> GetOrdersBySubscription(Guid subscriptionId)
        {
            var orders = await _service.GetOrdersBySubscriptionAsync(subscriptionId);
            return Ok(orders);
        }
    }
}
