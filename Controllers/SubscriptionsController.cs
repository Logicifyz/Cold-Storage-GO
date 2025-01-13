using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly DbContexts _context;  // ✅ Ensure the DbContext is defined here


        public SubscriptionsController(SubscriptionService subscriptionService, DbContexts context)
        {
            _subscriptionService = subscriptionService;
            _context = context;  // ✅ Properly injected here
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserSubscription(Guid userId)
        {
            var subscription = await _subscriptionService.GetSubscriptionByUserIdAsync(userId);
            if (subscription == null)
            {
                return NotFound("No subscription found for the given user.");
            }
            return Ok(subscription);
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

        // ✅ Toggle Auto-Renewal API
        [HttpPut("toggle-autorenewal/{subscriptionId}")]
        public async Task<IActionResult> ToggleAutoRenewal(Guid subscriptionId)
        {
            try
            {
                await _subscriptionService.ToggleAutoRenewalAsync(subscriptionId);
                return Ok("Auto-Renewal status updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }


        // ✅ Freeze Subscription
        [HttpPut("toggle-freeze/{subscriptionId}")]
        public async Task<IActionResult> ToggleFreeze(Guid subscriptionId)
        {
            try
            {
                var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
                if (subscription == null) return NotFound("Subscription not found.");

                if ((bool)subscription.IsFrozen)
                {
                    await _subscriptionService.UnfreezeSubscriptionAsync(subscriptionId);
                    return Ok("Subscription unfrozen successfully.");
                }
                else
                {
                    await _subscriptionService.FreezeSubscriptionAsync(subscriptionId);
                    return Ok("Subscription frozen successfully.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpDelete("cancel/{subscriptionId}")]
        public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
        {
            try
            {
                var subscription = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    return NotFound("Subscription not found.");
                }

                await _subscriptionService.CancelSubscriptionAsync(subscriptionId);
                return Ok("Subscription has been successfully canceled.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error canceling subscription: {ex.Message}");
            }
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