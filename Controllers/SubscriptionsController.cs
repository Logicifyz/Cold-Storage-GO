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

        // ✅ Cancel a Subscription without deleting the record (Status Change Only)
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

                // ✅ Update status to canceled instead of deleting
                subscription.Status = "Canceled";
                var subscriptionEvent = new SubscriptionEvent
                {
                    SubscriptionId = subscription.SubscriptionId,
                    UserId = subscription.UserId,
                    EventType = "Canceled",
                    Details = "Subscription canceled by user request."
                };

                _context.SubscriptionEvents.Add(subscriptionEvent);
                await _subscriptionService.UpdateSubscriptionAsync(subscription);


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
        // ✅ Fetch the latest subscription for the current user
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestSubscription()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is missing.");
            }

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (session == null)
            {
                return Unauthorized("Invalid or expired session.");
            }

            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == session.UserId && s.Status == "Active")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
                return NotFound("No active subscriptions found.");

            return Ok(new
            {
                subscription.SubscriptionId,
                subscription.SubscriptionType,
                subscription.SubscriptionChoice
            });
        }
        [HttpGet("active/{userId}")]
        public async Task<IActionResult> UserHasActiveSubscription(Guid userId)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID missing. Please log in again.");
            }

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId);

            if (userSession == null)
            {
                return Unauthorized("Invalid session. Please log in again.");
            }

            // ✅ Checking Active Status More Explicitly
            var hasActiveSubscription = await _context.Subscriptions
                .AnyAsync(s => s.UserId == userSession.UserId && s.Status == "Active");

            return Ok(new { hasActiveSubscription });
        }


        [HttpGet("history")]
        public async Task<IActionResult> GetSubscriptionHistory()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID missing. Please log in again.");
            }

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId);

            if (userSession == null)
            {
                return Unauthorized("Invalid session. Please log in again.");
            }

            // ✅ Fetch all expired and canceled subscriptions for the user
            var subscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userSession.UserId && s.Status != "Active")
                .OrderByDescending(s => s.EndDate)
                .ToListAsync();

            if (!subscriptions.Any())
            {
                return NotFound("No subscription history found.");
            }

            return Ok(subscriptions);
        }


    }
}