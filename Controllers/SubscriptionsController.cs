using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _service;

        public SubscriptionsController(SubscriptionService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            await _service.CreateSubscriptionAsync(request.UserId, request.MealKitId, request.Frequency, request.AutoRenewal);
            return Ok("Subscription created successfully!");
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserSubscriptions(Guid userId)
        {
            var subscriptions = await _service.GetUserSubscriptionsAsync(userId);
            return Ok(subscriptions);
        }
    }
}
