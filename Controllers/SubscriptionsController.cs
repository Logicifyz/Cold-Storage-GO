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
        private readonly NotificationService _notificationService;

        public SubscriptionsController(SubscriptionService subscriptionService, DbContexts context, NotificationService notificationService)
        {
            _subscriptionService = subscriptionService;
            _context = context;  // ✅ Properly injected here
            _notificationService = notificationService;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserSubscription([FromQuery] Guid userId)
        {
            var subscription = await _context.Subscriptions
        .Where(s => s.UserId == userId && s.Status != "Canceled" && s.Status != "Expired") // 🔥 Exclude canceled subscriptions
        .FirstOrDefaultAsync();

            if (subscription == null)
                return NotFound("Subscription not found.");

            // Get all freeze schedules for this subscription
            var today = DateTime.UtcNow.Date;
            bool isCurrentlyFrozen = await _context.SubscriptionFreezeHistories
                .AnyAsync(f => f.SubscriptionId == subscription.SubscriptionId &&
                               f.FreezeStartDate <= today && f.FreezeEndDate >= today);

            // Update isFrozen dynamically if needed
            if (subscription.IsFrozen != isCurrentlyFrozen)
            {
                subscription.IsFrozen = isCurrentlyFrozen;
                await _context.SaveChangesAsync();
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
            // Analytics tracking: Log subscription creation event
            var newSubscription = await _context.Subscriptions
                .Where(s => s.UserId == request.UserId)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();
            if (newSubscription != null)
            {
                _context.SubscriptionEvents.Add(new SubscriptionEvent
                {
                    SubscriptionId = newSubscription.SubscriptionId,
                    UserId = newSubscription.UserId,
                    EventType = "Created",
                    EventTime = DateTime.UtcNow,
                    Details = "Subscription created successfully."
                });
                await _context.SaveChangesAsync();
            }
            string notificationTitle = "New Subscription Created";
            string notificationContent = $"Your Subscription has been created successfully.";
            await _notificationService.CreateNotification(request.UserId, "Support", notificationTitle, notificationContent);

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

        // ✅ Cancel a Subscription without deleting the record (Status Change Only)
        [HttpDelete("cancel/{subscriptionId}")]
        public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
        {
            try
            {
                await _subscriptionService.CancelSubscriptionAsync(subscriptionId); // ✅ Now it actually cancels the subscription
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

        // ✅ Schedule a freeze for a future date
        [HttpPost("schedule-freeze/{subscriptionId}")]
        public async Task<IActionResult> ScheduleFreeze(Guid subscriptionId, [FromBody] ScheduleFreezeRequest request)
        {

            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null) return NotFound("Subscription not found.");
            DateTime minFreezeStartDate = subscription.StartDate.AddDays(1);
            if (request.StartDate < minFreezeStartDate)
            {
                return BadRequest($"Freeze start date must be at least {minFreezeStartDate:yyyy-MM-dd}.");
            }
            // Check if the freeze start or end date is after the subscription's end date
            if (request.StartDate > subscription.EndDate || request.EndDate > subscription.EndDate)
            {
                return BadRequest("Freeze dates cannot be after the subscription's end date.");
            }

            if (request.StartDate >= request.EndDate)
            {
                return BadRequest("End date must be after start date.");
            }

            // Check for overlapping freezes
            var overlappingFreeze = await _context.SubscriptionFreezeHistories
                .AnyAsync(f => f.SubscriptionId == subscriptionId &&
                               ((request.StartDate >= f.FreezeStartDate && request.StartDate <= f.FreezeEndDate) ||
                                (request.EndDate >= f.FreezeStartDate && request.EndDate <= f.FreezeEndDate) ||
                                (request.StartDate <= f.FreezeStartDate && request.EndDate >= f.FreezeEndDate)));

            if (overlappingFreeze)
            {
                return BadRequest("Freeze period overlaps with an existing scheduled freeze.");
            }

            // Add the new freeze period
            var freezeHistory = new SubscriptionFreezeHistory
            {
                SubscriptionId = subscriptionId,
                FreezeStartDate = request.StartDate,
                FreezeEndDate = request.EndDate
            };

            _context.SubscriptionFreezeHistories.Add(freezeHistory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Freeze scheduled successfully.", startDate = request.StartDate, endDate = request.EndDate });
        }

        // ✅ Cancel a scheduled freeze
        [HttpDelete("cancel-scheduled-freeze/{subscriptionId}/{freezeId}")]
        public async Task<IActionResult> CancelScheduledFreeze(Guid subscriptionId, Guid freezeId)
        {
            try
            {
                var freezeRecord = await _context.SubscriptionFreezeHistories
                    .FirstOrDefaultAsync(f => f.SubscriptionId == subscriptionId && f.FreezeId == freezeId);

                if (freezeRecord == null)
                {
                    return NotFound(new { message = "No scheduled freeze found to cancel." });
                }

                // Case 1: If the freeze has not started yet, just delete it
                if (freezeRecord.FreezeStartDate > DateTime.UtcNow)
                {
                    _context.SubscriptionFreezeHistories.Remove(freezeRecord);
                }
                else
                {
                    // Case 2: If the freeze is already active, update the freeze end date to today + 1
                    freezeRecord.FreezeEndDate = DateTime.UtcNow.Date.AddDays(1);

                    // Unfreeze the subscription if it's currently frozen
                    var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
                    if (subscription != null && subscription.IsFrozen == true)
                    {
                        subscription.IsFrozen = false;
                        _context.Subscriptions.Update(subscription);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Scheduled freeze has been canceled." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("scheduled-freezes/{subscriptionId}")]
        public async Task<IActionResult> GetScheduledFreezes(Guid subscriptionId)
        {
            var freezes = await _context.SubscriptionFreezeHistories
                .Where(f => f.SubscriptionId == subscriptionId && f.FreezeEndDate >= DateTime.UtcNow.Date)
                .OrderBy(f => f.FreezeStartDate)
                .Select(f => new
                {
                    FreezeId = f.FreezeId,  // ✅ Include FreezeId
                    StartDate = f.FreezeStartDate,
                    EndDate = f.FreezeEndDate
                })
                .ToListAsync();

            return Ok(freezes);
        }

        [HttpGet("recommendation/{userId}")]
        public async Task<IActionResult> GetSmartRecommendation(Guid userId)
        {
            var activeSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

            if (activeSubscription == null)
            {
                return NotFound(new { message = "No active subscription found." });
            }

            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

            // Get past subscriptions (excluding the current one) within the last 3 months
            var subscriptionHistory = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.SubscriptionId != activeSubscription.SubscriptionId && s.EndDate >= threeMonthsAgo)
                .OrderByDescending(s => s.EndDate)
                .ToListAsync();

            var freezeCount = await _context.SubscriptionFreezeHistories
                .Where(f => f.SubscriptionId == activeSubscription.SubscriptionId && f.FreezeStartDate >= threeMonthsAgo)
                .CountAsync();

            int cancellationCount = subscriptionHistory.Count(s => s.Status == "Canceled");

            // ✅ Check last 3 meal choices (SubscriptionChoice) instead of SubscriptionType
            var lastThreeChoices = subscriptionHistory
                .Select(s => s.SubscriptionChoice)
                .Distinct()
                .Take(3)
                .ToList();

            bool frequentMealSwitching = lastThreeChoices.Count == 3; // 3 different meal choices in a row

            var allChoices = new List<string> { "Vegetarian", "Pescatarian", "Halal", "Keto", "Vegan", "Gluten-Free" };
            var untriedChoices = allChoices.Except(lastThreeChoices).ToList();

            string recommendedChoice = activeSubscription.SubscriptionChoice;
            string reason = "No changes needed.";

            // ✅ Prioritization: Freeze & Cancellation first, then meal switching
            if (freezeCount >= 3 && cancellationCount >= 2)
            {
                recommendedChoice = "Pay-Per-Use";
                reason = "You've frozen and canceled multiple times. Pay-Per-Use may give you flexibility.";
            }
            else if (freezeCount >= 3)
            {
                recommendedChoice = "Weekly";
                reason = "You froze your meals frequently. A Weekly plan may offer better flexibility.";
            }
            else if (cancellationCount >= 2)
            {
                recommendedChoice = "Pay-Per-Use";
                reason = "You've canceled 2+ times. A Pay-Per-Use plan may be more suitable.";
            }
            else if (frequentMealSwitching && untriedChoices.Count > 0)
            {
                recommendedChoice = string.Join(", ", untriedChoices);
                reason = "You frequently switch meal plans! Try these new options.";
            }

            return Ok(new
            {
                recommendedChoice,
                reason
            });
        }

        [HttpGet("analytics/cancellation-rate")]
        public async Task<IActionResult> GetCancellationRate()
        {
            var cancellationRate = await _subscriptionService.GetCancellationRateAsync();
            return Ok(new { cancellationRate });
        }

        [HttpGet("analytics/frozen-rate")]
        public async Task<IActionResult> GetFrozenSubscriptionRate()
        {
            var frozenRate = await _subscriptionService.GetFrozenSubscriptionRateAsync();
            return Ok(new { frozenRate });
        }

        [HttpGet("analytics/popular-choices")]
        public async Task<IActionResult> GetPopularSubscriptionChoices()
        {
            var popularChoices = await _subscriptionService.GetPopularSubscriptionChoicesAsync();
            return Ok(popularChoices);
        }

        [HttpGet("analytics/popular-types")]
        public async Task<IActionResult> GetPopularSubscriptionTypes()
        {
            var popularTypes = await _subscriptionService.GetPopularSubscriptionTypesAsync();
            return Ok(popularTypes);
        }

        [HttpGet("analytics/summary")]
        public async Task<IActionResult> GetSubscriptionSummary()
        {
            var summary = await _subscriptionService.GetSubscriptionSummaryAsync();
            return Ok(summary);
        }
    }
}