using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly DbContexts _context;
        private readonly EmailService _emailService;

        public AccountController(DbContexts context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Check if the session exists and is active
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
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

            // Verify the current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Unauthorized("Current password is incorrect.");
            }

            // Check if the new password and confirmation match
            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest("New password and confirmation do not match.");
            }

            // Validate new password strength (optional, example for strong passwords)
            if (request.NewPassword.Length < 8)
            {
                return BadRequest("Password must be at least 8 characters long.");
            }

            // Hash the new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Update the user password in the database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully." });
        }
        // Endpoint to delete the account and remove associated data
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            // Get the currently authenticated user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            var user = await _context.Users
                .Include(u => u.UserAdministration)
                .FirstOrDefaultAsync(u => u.UserId == session.UserId);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Delete the user administration data
            if (user.UserAdministration != null)
            {
                _context.UserAdministration.Remove(user.UserAdministration);
            }

            // Delete the user's UserSessions
            var userUserSessions = await _context.UserSessions.Where(s => s.UserId == user.UserId).ToListAsync();
            _context.UserSessions.RemoveRange(userUserSessions);

            // Delete the user's profile and other related data
            var userProfile = await _context.UserProfiles.Where(up => up.UserId == user.UserId).FirstOrDefaultAsync();
            if (userProfile != null)
            {
                _context.UserProfiles.Remove(userProfile);
            }

            // Delete the user itself
            _context.Users.Remove(user);

            // Save all changes to the database
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Account and all associated data deleted successfully." });
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            // Get the currently authenticated user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            // Retrieve the user profile associated with the current user
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == session.UserId);
            if (userProfile == null)
            {
                return NotFound("User profile not found.");
            }

            // Update the profile with the provided details
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                userProfile.FullName = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                userProfile.PhoneNumber = request.PhoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(request.StreetAddress))
            {
                userProfile.StreetAddress = request.StreetAddress;
            }

            if (!string.IsNullOrWhiteSpace(request.PostalCode))
            {
                userProfile.PostalCode = request.PostalCode;
            }

            // Handle profile picture upload (optional)
            if (request.ProfilePicture != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await request.ProfilePicture.CopyToAsync(memoryStream);
                    userProfile.ProfilePicture = memoryStream.ToArray();  // Store as a byte array
                }
            }

            // Save changes to the database
            _context.UserProfiles.Update(userProfile);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Profile updated successfully." });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Get the currently authenticated user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            // Retrieve the user associated with the session
            var user = await _context.Users
                .Include(u => u.UserAdministration)  // If you need user-related data like UserAdministration
                .FirstOrDefaultAsync(u => u.UserId == session.UserId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Retrieve the user profile associated with the current user
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == session.UserId);

            if (userProfile == null)
            {
                return NotFound("User profile not found.");
            }

            // Create a response model with the necessary profile data
            var profileResponse = new
            {
                UserId = user.UserId,
                FullName = userProfile.FullName,
                PhoneNumber = userProfile.PhoneNumber,
                StreetAddress = userProfile.StreetAddress,
                PostalCode = userProfile.PostalCode,
                ProfilePicture = userProfile.ProfilePicture
            };

            return Ok(profileResponse);
        }

        [HttpGet("profile/{username}")]
        public async Task<IActionResult> GetOtherProfile(string username)
        {
            var userProfile = await _context.UserProfiles
                .Include(up => up.User)
                .FirstOrDefaultAsync(up => up.User.Username == username);

            if (userProfile == null)
            {
                return NotFound("User profile not found.");
            }

            var profileResponse = new
            {
                Username = userProfile.User.Username,  // Include the username
                ProfilePicture = userProfile.ProfilePicture  // Include only the profile picture
            };

            return Ok(profileResponse);
        }

        // Follow API
        [HttpPost("follow")]
        public async Task<IActionResult> Follow([FromBody] FollowRequest request)
        {
            // Get the current user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == session.UserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Get the followed user based on the username
            var followedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (followedUser == null)
            {
                return NotFound("Followed user not found.");
            }

            if (user.UserId == followedUser.UserId)
            {
                return BadRequest("You cannot follow yourself.");
            }

            // Check if the user is already following the followed user
            var alreadyFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == user.UserId && f.FollowedId == followedUser.UserId);

            if (alreadyFollowing)
            {
                return BadRequest("You are already following this user.");
            }

            // Add the follow relationship
            var follow = new Follows
            {
                FollowerId = user.UserId,
                FollowedId = followedUser.UserId
            };

            await _context.Follows.AddAsync(follow);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Successfully followed the user." });
        }

        // Unfollow API
        [HttpPost("unfollow")]
        public async Task<IActionResult> Unfollow([FromBody] FollowRequest request)
        {
            // Get the current user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == session.UserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Get the followed user based on the username
            var followedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (followedUser == null)
            {
                return NotFound("Followed user not found.");
            }
            if (user.UserId == followedUser.UserId)
            {
                return BadRequest("You cannot unfollow yourself.");
            }
            // Check if the user is following the followed user
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == user.UserId && f.FollowedId == followedUser.UserId);

            if (follow == null)
            {
                return BadRequest("You are not following this user.");
            }

            // Remove the follow relationship
            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Successfully unfollowed the user." });
        }

        // Get Following List API
        [HttpGet("following")]
        public async Task<IActionResult> GetFollowingList()
        {
            // Get the current user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            var user = await _context.Users
            .Include(u => u.Following)
                .ThenInclude(f => f.Followed) // Make sure Followed is included
            .FirstOrDefaultAsync(u => u.UserId == session.UserId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var followingList = user.Following
                .Where(f => f.Followed != null) // Ensure Followed is not null
                .Select(f => new
                {
                    f.Followed.Username,
                    f.Followed.UserId
                })
                .ToList();

            return Ok(followingList);
        }

        // Get Followers List API
        [HttpGet("followers")]
        public async Task<IActionResult> GetFollowersList()
        {
            // Get the current user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == HttpContext.Request.Headers["SessionId"]);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            var user = await _context.Users
        .Include(u => u.Followers)
            .ThenInclude(f => f.Follower) // Make sure Follower is included
        .FirstOrDefaultAsync(u => u.UserId == session.UserId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var followersList = user.Followers
                .Where(f => f.Follower != null) // Ensure Follower is not null
                .Select(f => new
                {
                    f.Follower.Username,
                    f.Follower.UserId
                })
                .ToList();

            return Ok(followersList);

        }



        // Request Model for password change
        public class ChangePasswordRequest
        {

            [Required]
            public string CurrentPassword { get; set; }

            [Required]
            public string NewPassword { get; set; }

            [Required]
            [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
            public string ConfirmPassword { get; set; }
        }
        // Request model for updating the profile
        public class UpdateProfileRequest
        {
            public string FullName { get; set; }
            public string PhoneNumber { get; set; }
            public string StreetAddress { get; set; }
            public string PostalCode { get; set; }
            public IFormFile? ProfilePicture { get; set; }  // Optional profile picture

        }

        // Request Model for Follow/Unfollow
        public class FollowRequest
        {
            [Required]
            public string Username { get; set; }
        }
    }
}
    