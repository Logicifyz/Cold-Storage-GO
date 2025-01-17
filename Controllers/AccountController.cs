using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly DbContexts _context;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(DbContexts context, EmailService emailService, ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;

        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Check if the session exists and is active
            var sessionId = HttpContext.Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session not found.");
            }

            // Check if the session exists and is active
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == sessionId);

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
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            // Try to get SessionId from request headers first
            var sessionId = HttpContext.Request.Cookies["SessionId"];

            // Check if the session ID exists in the cookies
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized(new { message = "Session expired or not found." });
            }

            // Get the currently authenticated user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == sessionId);
            if (session == null)
            {
                return Unauthorized(new { message = "Session expired or not found." });
            }

            // Fetch the user associated with the session
            var user = await _context.Users
                .Include(u => u.UserAdministration)
                .FirstOrDefaultAsync(u => u.UserId == session.UserId);

            if (user == null)
            {
                return BadRequest(new { message = "User not found." });
            }

            // Verify the password provided in the request
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Incorrect password." });
            }

            // Delete the user administration data if exists
            if (user.UserAdministration != null)
            {
                _context.UserAdministration.Remove(user.UserAdministration);
            }

            // Remove any active sessions related to the user
            var userUserSessions = await _context.UserSessions.Where(s => s.UserId == user.UserId).ToListAsync();
            _context.UserSessions.RemoveRange(userUserSessions);

            // Delete the user's profile if it exists
            var userProfile = await _context.UserProfiles.Where(up => up.UserId == user.UserId).FirstOrDefaultAsync();
            if (userProfile != null)
            {
                _context.UserProfiles.Remove(userProfile);
            }

            // Remove the user from the database
            _context.Users.Remove(user);

            // Save all changes to the database
            await _context.SaveChangesAsync();

            // Return a success message
            return Ok(new { message = "Account and all associated data deleted successfully." });
        }

        // Helper method to verify the password
        private bool VerifyPassword(string inputPassword, string storedPasswordHash)
        {
            // Assuming you're using a hashing algorithm like BCrypt
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedPasswordHash);
        }

        // Request model to accept the password in the request body




        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            _logger.LogDebug("Received request to update profile.");

            try
            {
                // Get the currently authenticated user based on the session
                var sessionId = HttpContext.Request.Cookies["SessionId"];
                _logger.LogDebug($"Session ID retrieved: {sessionId}");

                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogDebug("Session ID is empty or null.");
                    return Unauthorized(new { Message = "Session expired or not found." });
                }

                // Get the currently authenticated user based on the session
                var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == sessionId);
                if (session == null)
                {
                    _logger.LogDebug("User session not found.");
                    return Unauthorized(new { Message = "Session expired or not found." });
                }
                _logger.LogDebug($"Session found for user {session.UserId}.");

                // Retrieve the user profile associated with the current user
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == session.UserId);
                if (userProfile == null)
                {
                    _logger.LogDebug($"User profile not found for user {session.UserId}.");
                    return NotFound(new { Message = "User profile not found." });
                }
                _logger.LogDebug($"User profile found for user {session.UserId}.");

                // Retrieve the user details from the User table
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == session.UserId);
                if (user == null)
                {
                    _logger.LogDebug($"User not found for user ID {session.UserId}.");
                    return NotFound(new { Message = "User not found." });
                }
                _logger.LogDebug($"User details found for user {user.Username}.");

                // Check if the username is already taken, unless the provided username is the same as the current one
                if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
                {
                    _logger.LogDebug($"Checking if username {request.Username} is taken.");
                    var usernameTaken = await _context.Users.AnyAsync(u => u.Username == request.Username);
                    if (usernameTaken)
                    {
                        _logger.LogDebug($"Username {request.Username} is already taken.");
                        return Conflict(new { Message = "Username is already taken." });
                    }
                }

                // Check if the provided email is different from the current user's email, 
                // and if it is, check if it is already taken.
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    _logger.LogDebug($"Checking if email {request.Email} is taken.");
                    var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email);
                    if (emailTaken)
                    {
                        _logger.LogDebug($"Email {request.Email} is already taken.");
                        return Conflict(new { Message = "Email is already taken." });
                    }
                }

                // Update the profile with the provided details
                if (!string.IsNullOrWhiteSpace(request.FullName))
                {
                    _logger.LogDebug($"Updating full name to {request.FullName}.");
                    userProfile.FullName = request.FullName;
                }

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    _logger.LogDebug($"Updating phone number to {request.PhoneNumber}.");
                    userProfile.PhoneNumber = request.PhoneNumber;
                }

                // Handle profile picture upload (optional)
                if (request.ProfilePicture != null)
                {
                    _logger.LogDebug("Uploading new profile picture.");
                    using (var memoryStream = new MemoryStream())
                    {
                        await request.ProfilePicture.CopyToAsync(memoryStream);
                        userProfile.ProfilePicture = memoryStream.ToArray();  // Store as a byte array
                    }
                }

                // Update username and email if they are provided and not already taken
                if (!string.IsNullOrWhiteSpace(request.Username))
                {
                    // If the username is different from the current username, update it
                    _logger.LogDebug($"Updating username to {request.Username}.");
                    user.Username = request.Username;
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    // If the email is different from the current email, update it
                    _logger.LogDebug($"Updating email to {request.Email}.");
                    user.Email = request.Email;
                }

                // Save changes to the database
                _logger.LogDebug("Saving changes to the database.");
                _context.UserProfiles.Update(userProfile);
                _context.Users.Update(user); // Update the User table with the new username/email
                await _context.SaveChangesAsync();

                _logger.LogDebug("Profile updated successfully.");
                return Ok(new { Message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while updating the profile: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }




        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Get the currently authenticated user based on the session
            var sessionId = HttpContext.Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session expired or not found.");
            }

            // Get the currently authenticated user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == sessionId);
            if (session == null)
            {
                return Unauthorized("Session expired or not found.");
            }

            // Retrieve the user associated with the session
            var user = await _context.Users
                .Include(u => u.UserAdministration)  // Include the UserAdministration to get the verification status
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

            // Create a response model with the necessary profile data, including username, email, and verified status
            var profileResponse = new
            {
                UserId = user.UserId,
                Username = user.Username,  // Include username
                Email = user.Email,        // Include email
                FullName = userProfile.FullName,
                PhoneNumber = userProfile.PhoneNumber,
                StreetAddress = userProfile.StreetAddress,
                PostalCode = userProfile.PostalCode,
                ProfilePicture = userProfile.ProfilePicture,
                Verified = user.UserAdministration.Verified  // Add the verified status from the UserAdministration
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
            var sessionId = HttpContext.Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session expired or not found.");
            }

            // Get the currently authenticated user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == sessionId);
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
            var sessionId = HttpContext.Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session expired or not found.");
            }

            // Get the currently authenticated user based on the session
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.IsActive && s.UserSessionId == sessionId);
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
        [HttpGet("following/{username}")]
        public async Task<IActionResult> GetFollowingList(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required.");
            }

            var user = await _context.Users
                .Include(u => u.Following)
                    .ThenInclude(f => f.Followed)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var followingList = user.Following
                .Where(f => f.Followed != null)
                .Select(f => new
                {
                    f.Followed.Username,
                    f.Followed.UserId
                })
                .ToList();

            return Ok(followingList);
        }

        [HttpGet("followers/{username}")]
        public async Task<IActionResult> GetFollowersList(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required.");
            }

            var user = await _context.Users
                .Include(u => u.Followers)
                    .ThenInclude(f => f.Follower)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var followersList = user.Followers
                .Where(f => f.Follower != null)
                .Select(f => new
                {
                    f.Follower.Username,
                    f.Follower.UserId
                })
                .ToList();

            return Ok(followersList);
        }


        public class DeleteAccountRequest
        {
            [Required]
            public string Password { get; set; }
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
            public string? FullName { get; set; }
            public string? PhoneNumber { get; set; }

            // Optional profile picture
            public IFormFile? ProfilePicture { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Username is required.")]
            [StringLength(50, ErrorMessage = "Username can't be longer than 50 characters.")]
            public string Username { get; set; }
        }

        // Request Model for Follow/Unfollow
        public class FollowRequest
        {
            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; }
        }
    }
}
    