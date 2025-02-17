// StaffAccountController.cs
using Cold_Storage_GO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class StaffAccountController : ControllerBase
    {
        private readonly DbContexts _context;

        public StaffAccountController(DbContexts context)
        {
            _context = context;
        }

        private async Task<bool> ValidateStaffSession()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return false;
            }

            var staffSession = await _context.StaffSessions
                .FirstOrDefaultAsync(ss => ss.StaffSessionId == sessionId && ss.IsActive);

            if (staffSession == null)
            {
                return false;
            }

            var staff = await _context.Staff
                .FirstOrDefaultAsync(u => u.StaffId == staffSession.StaffId && u.Role == "staff");

            return staff != null;
        }

        // GET: api/StaffAccount/GetAllUsers
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers(
            string? name = null,
            string? email = null,
            bool? isActive = null)
        {
            if (!await ValidateStaffSession())
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            var query = _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.UserAdministration)
                .AsQueryable();

            // Filtering
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(u => u.UserProfile.FullName.Contains(name));
            }
            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(u => u.Email.Contains(email));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.UserAdministration.Activation == isActive.Value);
            }

            var users = await query
                .Select(u => new
                {
                    u.UserId,
                    u.Email,
                    u.Username,
                    FullName = u.UserProfile.FullName,
                    PhoneNumber = u.UserProfile.PhoneNumber,
                    IsActive = u.UserAdministration.Activation,
                    Verified = u.UserAdministration.Verified,
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/StaffAccount/GetUserDetails/{userId}
        [HttpGet("GetUserDetails/{userId}")]
        public async Task<IActionResult> GetUserDetails(Guid userId)
        {
            if (!await ValidateStaffSession())
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.UserAdministration)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var isOnline = await _context.UserSessions
                .AnyAsync(s => s.UserId == userId && s.IsActive);

            var userDetails = new
            {
                user.UserId,
                user.Email,
                user.Username,
                user.PasswordHash,
                user.Role,
                Profile = new
                {
                    user.UserProfile?.ProfileId,
                    user.UserProfile?.FullName,
                    user.UserProfile?.PhoneNumber,
                    user.UserProfile?.StreetAddress,
                    user.UserProfile?.PostalCode,
                    user.UserProfile?.SubscriptionStatus,
                    user.UserProfile?.ProfilePicture
                },
                Administration = new
                {
                    user.UserAdministration?.Verified,
                    user.UserAdministration?.VerificationToken,
                    user.UserAdministration?.PasswordResetToken,
                    user.UserAdministration?.Activation,
                    user.UserAdministration?.FailedLoginAttempts,
                    user.UserAdministration?.LockoutUntil,
                    user.UserAdministration?.LastFailedLogin
                },
                IsOnline = isOnline
            };

            return Ok(userDetails);
        }

        [HttpPut("DeactivateAccount/{userId}")]
        public async Task<IActionResult> DeactivateAccount(Guid userId)
        {
            if (!await ValidateStaffSession())
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            var userAdministration = await _context.UserAdministration
                .FirstOrDefaultAsync(ua => ua.UserId == userId);

            if (userAdministration == null)
            {
                return NotFound(new { message = "User not found" });
            }

            userAdministration.Activation = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account deactivated successfully" });
        }

        [HttpPut("ActivateAccount/{userId}")]
        public async Task<IActionResult> ActivateAccount(Guid userId)
        {
            if (!await ValidateStaffSession())
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            var userAdministration = await _context.UserAdministration
                .FirstOrDefaultAsync(ua => ua.UserId == userId);

            if (userAdministration == null)
            {
                return NotFound(new { message = "User not found" });
            }

            userAdministration.Activation = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account activated successfully" });
        }
    }
}
