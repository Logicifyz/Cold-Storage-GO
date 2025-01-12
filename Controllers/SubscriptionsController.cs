using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionsController(SubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // ✅ Updated: Now includes SubscriptionChoice
        [HttpPost]
        public async Task<IActionResult> CreateSubscription(CreateSubscriptionRequest request)
        {
            await _subscriptionService.CreateSubscriptionAsync(
                request.UserId,
                request.MealKitId,
                request.Frequency,
                request.DeliveryTimeSlot,
                request.SubscriptionType,
                request.SubscriptionChoice, // New field added here
                request.Price
            );
            return Ok("Subscription created successfully");
        }

        [HttpPut("freeze/{subscriptionId}")]
        public async Task<IActionResult> FreezeSubscription(Guid subscriptionId)
        {
            await _subscriptionService.FreezeSubscriptionAsync(subscriptionId);
            return Ok("Subscription frozen");
        }

        [HttpDelete("{subscriptionId}")]
        public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
        {
            await _subscriptionService.CancelSubscriptionAsync(subscriptionId);
            return Ok("Subscription canceled");
        }

        // ✅ New: Get Subscriptions by SubscriptionChoice
        [HttpGet("choice/{subscriptionChoice}")]
        public async Task<IActionResult> GetSubscriptionsByChoice(string subscriptionChoice)
        {
            var subscriptions = await _subscriptionService.GetSubscriptionsByChoiceAsync(subscriptionChoice);
            if (!subscriptions.Any())
            {
                return NotFound("No subscriptions found with this choice.");
            }
            return Ok(subscriptions);
        }

        // ✅ Staff Management: Get All Subscriptions
        [HttpGet("staff/all")]
        public async Task<IActionResult> GetAllSubscriptions()
        {
            var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            return Ok(subscriptions);
        }

        [HttpGet("staff/status/{status}")]
        public async Task<IActionResult> GetSubscriptionsByStatus(string status)
        {
            if (!bool.TryParse(status, out bool isFrozenBool))
            {
                return BadRequest("Invalid input. Please use 'true' or 'false' for the subscription status.");
            }

            var subscriptions = await _subscriptionService.GetSubscriptionsByStatusAsync(isFrozenBool);
            return Ok(subscriptions);
        }

        [HttpGet("staff/search")]
        public async Task<IActionResult> SearchSubscriptions([FromQuery] string query)
        {
            var subscriptions = await _subscriptionService.SearchSubscriptionsAsync(query);
            return Ok(subscriptions);
        }

        [HttpPut("update/{subscriptionId}")]
        public async Task<IActionResult> UpdateSubscription(Guid subscriptionId, [FromBody] UpdateSubscriptionRequest request)
        {
            await _subscriptionService.UpdateSubscriptionStatusAsync(subscriptionId, request);
            return Ok("Subscription updated successfully");
        }

    }
}