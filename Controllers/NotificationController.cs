using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly DbContexts _context;

        public NotificationController(DbContexts context)
        {
            _context = context;
        }

        // Helper method to verify session and get user
        private async Task<ActionResult<User>> VerifySession()
        {
            var sessionId = HttpContext.Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session not found.");
            }

            // Check if the session exists and is active
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == sessionId);

            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            // Retrieve the user related to the session
            var user = await _context.Users
                .Include(u => u.UserAdministration)
                .FirstOrDefaultAsync(u => u.UserId == session.UserId);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            return user;
        }

        // GET: api/Notification
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
    [FromQuery] bool? isRead = null,
    [FromQuery] string type = null,
    [FromQuery] string notificationId = null)  // Added notificationId query param
        {
            // Verify session and get user
            var userResult = await VerifySession();
            if (userResult.Result != null) return userResult.Result;

            var user = userResult.Value;

            var query = _context.Notifications
                .Where(n => n.UserId == user.UserId)  // Only get notifications for the current user
                .AsQueryable();

            // Apply filters if provided
            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(n => n.Type == type);
            }

            if (!string.IsNullOrEmpty(notificationId))  // Filter by notificationId if provided
            {
                query = query.Where(n => n.NotificationId.ToString() == notificationId);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            if (!notifications.Any())
            {
                return NotFound("No notifications found.");
            }

            return Ok(notifications);
        }


        // PUT: api/Notification/MarkAsRead/{notificationId}
        [HttpPut("MarkAsRead/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            // Verify session and get user
            var userResult = await VerifySession();
            if (userResult.Result != null) return userResult.Result;

            var user = userResult.Value;

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == user.UserId);

            if (notification == null)
            {
                return NotFound("Notification not found.");
            }

            notification.IsRead = true;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notification marked as read." });
        }
        // DELETE: api/Notification/DeleteRead
        [HttpDelete("DeleteRead")]
        public async Task<IActionResult> DeleteRead()
        {
            // Verify session and get user
            var userResult = await VerifySession();
            if (userResult.Result != null) return userResult.Result;

            var user = userResult.Value;

            // Find all read notifications for the current user
            var readNotifications = await _context.Notifications
                .Where(n => n.UserId == user.UserId && n.IsRead)
                .ToListAsync();

            if (!readNotifications.Any())
            {
                return NotFound("No read notifications to delete.");
            }

            // Remove all read notifications
            _context.Notifications.RemoveRange(readNotifications);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "All read notifications deleted successfully." });
        }

    }
}
