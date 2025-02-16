using Cold_Storage_GO;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using static AuthController;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DbContexts _context;
    private readonly EmailService _emailService;
    private readonly GoogleAuthService _googleAuthService;
    private readonly string _clientId = "869557804479-pv18rpo94fbpd6hatmns6m4nes5adih8.apps.googleusercontent.com";  // Replace with your actual Google client ID
    private readonly ILogger<AuthController> _logger;
        

    public AuthController(DbContexts context, EmailService emailService, GoogleAuthService googleAuthService, ILogger<AuthController> logger)
    {
        _context = context;
        _emailService = emailService;
        _googleAuthService = googleAuthService;
        _logger = logger;

    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if the model is valid
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Validation failed.", details = ModelState });
        }

        // Check if email is already taken
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { success = false, message = "Email is already in use." });
        }

        // Check if username is already taken
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new { success = false, message = "Username is already taken." });
        }

        // Password and Confirm Password do not match
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { success = false, message = "Password and confirm password do not match." });
        }

        try
        {
            // Create User
            var user = new User
            {
                UserId = Guid.NewGuid(), // Generate UUID for the user
                Email = request.Email,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // Hash password
                Role = "user" // Default role is "user"
            };

            // Create UserProfile
            var profile = new UserProfile
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                StreetAddress = request.StreetAddress,
                PostalCode = request.PostalCode,
                User = user // Link profile to user
            };

            var userAdmin = new UserAdministration
            {
                UserId = user.UserId,
                Verified = false,
                VerificationToken = Guid.NewGuid().ToString(), // Generate verification token
                Activation = true,
                FailedLoginAttempts = 0,
                PasswordResetToken = null // Initially no reset token
            };

            // Automatically create wallet for the user
            var wallet = new Wallet
            {
                WalletId = Guid.NewGuid(),
                UserId = user.UserId,
                CoinsEarned = 0,
                CoinsRedeemed = 0
            };

            // Add User and Profile to the database
            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            _context.UserProfiles.Add(profile);
            _context.UserAdministration.Add(userAdmin);

            await _context.SaveChangesAsync();

            // Create a session for the user by generating a session ID
            var sessionId = Guid.NewGuid().ToString(); // Generate a unique session ID

            var session = new UserSession
            {
                UserSessionId = sessionId,
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                Data = "{}",
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            // Set the session ID as a cookie in the response
            CookieService.SetCookie(HttpContext, "SessionId", sessionId);

            // Send verification email
            var verificationUrl = $"{Request.Scheme}://localhost:3000/verify-account/{userAdmin.VerificationToken}";

            // Send the verification email with the URL
            var emailSent = await _emailService.SendEmailAsync(
                request.Email,
                "Email Verification",
                $"Please verify your email by clicking on the following link: <a href='{verificationUrl}'>Verify Email</a>"
            );

            if (!emailSent)
            {
                return StatusCode(500, new { success = false, message = "Failed to send verification email." });
            }

            return Ok(new { success = true, message = "Registration successful. Please check your email for verification.", wallet });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An unexpected error occurred.", details = ex.Message });
        }
    }



    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
        .Include(u => u.UserProfile)
        .Include(u => u.UserAdministration)
        .FirstOrDefaultAsync(u => u.Email == request.Email);

        // Check if the user exists
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var userAdmin = user.UserAdministration;

        // Check if the account is deactivated
        if (!userAdmin.Activation)
        {
            return StatusCode(403, new { message = "Account is deactivated. Please contact support." });
        }


        // Check if account is locked
        if (userAdmin.LockoutUntil.HasValue && userAdmin.LockoutUntil > DateTime.UtcNow)
        {
            // Convert LockoutUntil to Singapore Time (UTC+8)
            var singaporeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var lockoutTime = TimeZoneInfo.ConvertTimeFromUtc(userAdmin.LockoutUntil.Value, singaporeTimeZone).ToString("yyyy-MM-dd HH:mm:ss");

            return StatusCode(403, new { message = $"Account is locked until {lockoutTime} SGT" });
        }


        // Reset failed login attempts if it has been more than 5 minutes since the last failed attempt
        if (userAdmin.LastFailedLogin.HasValue && userAdmin.LastFailedLogin.Value.AddMinutes(5) < DateTime.UtcNow)
        {
            userAdmin.FailedLoginAttempts = 0; // Reset the failed attempts
        }

        // Check if password is correct
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Increment failed login attempts
            userAdmin.FailedLoginAttempts++;
            userAdmin.LastFailedLogin = DateTime.UtcNow; // Record the time of the failed login attempt

            // If failed attempts exceed the limit, lock the account
            if (userAdmin.FailedLoginAttempts >= 5)
            {
                userAdmin.LockoutUntil = DateTime.UtcNow.AddMinutes(5); // Lock for 5 minutes
            }

            await _context.SaveChangesAsync();
            return Unauthorized(new { message = "Invalid email or password." });
        }

        userAdmin.FailedLoginAttempts = 0;
        userAdmin.LockoutUntil = null; // Clear lockout time
        userAdmin.LastFailedLogin = null; // Clear the last failed login time

        await _context.SaveChangesAsync();

        // Check for an existing active session
        var existingSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserId == user.UserId && s.IsActive);

        string sessionId;

        if (existingSession != null)
        {
            // Update LastAccessed for the existing session
            existingSession.LastAccessed = DateTime.UtcNow;
            _context.UserSessions.Update(existingSession);
            await _context.SaveChangesAsync();

            sessionId = existingSession.UserSessionId;
        }
        else
        {
            // Create a new session if none exists
            sessionId = Guid.NewGuid().ToString();
            var session = new UserSession
            {
                UserSessionId = sessionId,
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                Data = "{}",
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // Set the session ID in a cookie
        CookieService.SetCookie(HttpContext, "SessionId", sessionId);


        return Ok(new
        {
            success = true,
            message = "Login successful!",
            UserSessionId = sessionId,
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            Profile = new
            {
                user.UserProfile.FullName,
                user.UserProfile.PhoneNumber,
                user.UserProfile.StreetAddress,
                user.UserProfile.PostalCode
            }
        });
    }



    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.User.Email == request.Email);

        if (userAdmin == null)
            return BadRequest(new { Message = "No user found with the provided email." });

        // Generate password reset token
        userAdmin.PasswordResetToken = Guid.NewGuid().ToString();
        // LockoutUntil removed, it was previously set for token expiration, no longer required.

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();
        var verificationUrl = $"{Request.Scheme}://localhost:3000/resetpassword/{userAdmin.PasswordResetToken}";

        // Send the password reset token via email
        var emailSent = await _emailService.SendEmailAsync(
            request.Email,
            "Password Reset Request",
            $"Please reset your password by clicking on the following link: <a href='{verificationUrl}'>Reset Password</a>"
        );


        // Send the verification email with the URL
        
        if (!emailSent)
            return StatusCode(500, "Failed to send the email.");

        return Ok(new {success = true, Message = "Password reset token generated.", Token = userAdmin.PasswordResetToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetTokenRequest request)
    {
        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.PasswordResetToken == request.Token);

        if (userAdmin == null || string.IsNullOrEmpty(userAdmin.PasswordResetToken))
            return BadRequest(new { message = "Invalid or expired token." });

        // Reset the password and invalidate the reset token
        userAdmin.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        userAdmin.PasswordResetToken = null; // Nullify the token after use

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();

        return Ok(new { success =true, Message = "Password reset successful." });
    }

    // Endpoint for email verification
    [HttpPost("verify-email/{token}")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.VerificationToken == token);

        if (userAdmin == null)
            return BadRequest(new { Message = "Invalid or expired verification token." });

        // Mark the user as verified and invalidate the token
        userAdmin.Verified = true;
        userAdmin.VerificationToken = null;

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();
                                                                                                        
        return Ok(new { Message = "Email verified successfully." });
    }


    [HttpPost("request-verification-email")]
    public async Task<IActionResult> RequestVerificationEmail([FromBody] VerificationEmailRequest request)
    {
        // Retrieve the session ID from the cookies
        var sessionId = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized("User is not logged in or session has expired.");
        }

        // Check if the session is valid and active
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

        if (session == null)
        {
            return Unauthorized("User is not logged in or session has expired.");
        }

        // Retrieve the user related to the session and validate the email
        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.User.Email == request.Email);

        if (userAdmin == null)
        {
            return BadRequest("No user found with the provided email.");
        }

        // Ensure that the user is not already verified
        if (userAdmin.Verified)
        {
            return BadRequest("The user is already verified.");
        }

        // Generate a new verification token
        userAdmin.VerificationToken = Guid.NewGuid().ToString();

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();

        // Send the verification email
        var verificationUrl = $"{Request.Scheme}://localhost:3000/verify-account/{userAdmin.VerificationToken}";

        // Send the verification email with the URL
        var emailSent = await _emailService.SendEmailAsync(
            request.Email,
            "Email Verification",
            $"Please verify your email by clicking on the following link: <a href='{verificationUrl}'>Verify Email</a>"
        );

        if (!emailSent)
        {
            return StatusCode(500, "Failed to send the verification email.");
        }

        return Ok(new { Message = "A new verification email has been sent." });
    }

    [HttpPost("staff/login")]
    public async Task<IActionResult> StaffLogin([FromBody] LoginRequest request)
    {
        // Find staff member by email
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Email == request.Email);

        if (staff == null)
        {
            return Unauthorized("Staff not found.");
        }

        // Check if the staff member has the correct role
        if (staff.Role != "staff")
        {
            return Unauthorized("Invalid role.");
        }

        // Verify the password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, staff.Password))
        {
            return Unauthorized("Invalid password.");
        }

        // Check for an existing active session for the staff member
        var existingSession = await _context.StaffSessions
            .FirstOrDefaultAsync(s => s.StaffId == staff.StaffId && s.IsActive);

        string sessionId;

        if (existingSession != null)
        {
            // Log the existing session details
            _logger.LogInformation("Existing session found for StaffId: {StaffId}, SessionId: {SessionId}. Updating LastAccessed.", staff.StaffId, existingSession.StaffSessionId);

            // Update LastAccessed for the existing session
            existingSession.LastAccessed = DateTime.UtcNow;
            _context.StaffSessions.Update(existingSession);
            await _context.SaveChangesAsync();

            // Use the existing session ID
            sessionId = existingSession.StaffSessionId;
        }
        else
        {
            // Create a new session if none exists
            sessionId = Guid.NewGuid().ToString();
            var staffSession = new StaffSession
            {
                StaffSessionId = sessionId,
                StaffId = staff.StaffId,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                Data = "{}",
                IsActive = true
            };

            // Log the new session creation
            _logger.LogInformation("Creating a new session for StaffId: {StaffId}, SessionId: {SessionId}.", staff.StaffId, sessionId);

            _context.StaffSessions.Add(staffSession);
            await _context.SaveChangesAsync();
        }

        // Set the session ID in a cookie
        CookieService.SetCookie(HttpContext, "SessionId", sessionId);
        _logger.LogInformation("SessionId {SessionId} set in the cookie for StaffId: {StaffId}.", sessionId, staff.StaffId);

        // Return a response with the session and staff info
        return Ok(new
        {
            SessionId = sessionId,
            StaffId = staff.StaffId,
            Name = staff.Name,
            Email = staff.Email,
            Role = staff.Role
        });
    }


    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Retrieve the session ID from the cookies
        var sessionId = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new { success = false, message = "No active session found." });
        }

        // Find the active session for the user
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

        if (session == null)
        {
            return Unauthorized(new { success = false, message = "Session not found or already expired." });
        }

        // Mark the session as inactive
        session.IsActive = false;
        _context.UserSessions.Update(session);
        await _context.SaveChangesAsync();

        // Remove the session cookie
        CookieService.RemoveCookie(HttpContext, "SessionId");

        return Ok(new { success = true, message = "Logout successful." });
    }

    [HttpGet("check-session")]
    public async Task<IActionResult> CheckSession()
    {
        // Retrieve the SessionId from cookies
        var sessionId = HttpContext.Request.Cookies["SessionId"];

        if (string.IsNullOrEmpty(sessionId))
        {
            return Ok(new { sessionValid = false, reason = "NoSession" });
        }

        // Find the session by SessionId
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserSessionId == sessionId);

        if (session == null)
        {
            return Ok(new { sessionValid = false, reason = "SessionNotFound" });
        }

        // Check if the session is inactive
        if (!session.IsActive)
        {
            return Ok(new { sessionValid = false, reason = "SessionInactive" });
        }

        // Check if the session expired
        var sessionExpiry = session.LastAccessed.AddSeconds(60);
        if (DateTime.UtcNow > sessionExpiry)
        {
            // Mark session as inactive and remove it
            session.IsActive = false;
            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync();

            // Clear the session cookie
            CookieService.SetCookie(HttpContext, "SessionId", "", -1);

            return Ok(new { sessionValid = false, reason = "SessionExpired" });
        }

        // Update the last accessed time for the session
        session.LastAccessed = DateTime.UtcNow;
        _context.UserSessions.Update(session);
        await _context.SaveChangesAsync();

        // Retrieve the user associated with this session
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == session.UserId);

        if (user == null)
        {
            return Ok(new { sessionValid = false, reason = "UserNotFound" });
        }

        // Get the profile picture from UserProfiles table
        var userProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(up => up.UserId == user.UserId);

        // Convert blob to Base64 string
        string profilePicBase64 = null;
        if (userProfile?.ProfilePicture != null && userProfile.ProfilePicture.Length > 0)
        {
            profilePicBase64 = Convert.ToBase64String(userProfile.ProfilePicture);
        }

        // Return session validity, userId, username, and profilePic
        return Ok(new
        {
            sessionValid = true,
            userId = user.UserId,
            username = user.Username,
            profilePic = profilePicBase64
        });
    }








    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        // Verify Google ID token
        var googleUser = await VerifyGoogleTokenAsync(request.IdToken);

        if (googleUser == null)
        {
            return Unauthorized(new { message = "Invalid Google token" });
        }

        if (string.IsNullOrEmpty(googleUser.Email))
        {
            return BadRequest(new { message = "Google login did not return a valid email" });
        }

        // Check if the user exists in your database
        var user = await _context.Users
            .Include(u => u.UserProfile)
            .Include(u => u.UserAdministration)
            .FirstOrDefaultAsync(u => u.Email == googleUser.Email);

        if (user == null)
        {
            // User doesn't exist, create a new user
            user = new User
            {
                UserId = Guid.NewGuid(),
                Email = googleUser.Email,
                Username = googleUser.Name,
                Role = "user", // Default role is "user"
                PasswordHash = "google-login" // Password not required for Google sign-in
            };

            var profile = new UserProfile
            {
                FullName = googleUser.Name,
                PhoneNumber = "", // Optional to handle
                StreetAddress = "", // Optional to handle
                PostalCode = "", // Optional to handle
                User = user
            };

            var userAdmin = new UserAdministration
            {
                UserId = user.UserId,
                Verified = true, // Assuming the user is verified right after Google login
                VerificationToken = null, // No need for verification token
                Activation = true, // Mark as active
                FailedLoginAttempts = 0,
                LockoutUntil = null,
                PasswordResetToken = null // Initially no reset token
            };

            // Automatically create wallet for the user
            var wallet = new Wallet
            {
                WalletId = Guid.NewGuid(),
                UserId = user.UserId,
                CoinsEarned = 0,
                CoinsRedeemed = 0
            };

            // Add User, Profile, UserAdministration, and Wallet to the database
            _context.Users.Add(user);
            _context.UserProfiles.Add(profile);
            _context.UserAdministration.Add(userAdmin);
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        if (!user.UserAdministration.Activation)
        {
            return StatusCode(403, new { message = "Account is deactivated. Please contact support." });
        }


        // Check if password is set for the user
        if (string.IsNullOrEmpty(user.PasswordHash) || user.PasswordHash == "google-login")
        {
            // If password is not set, generate a reset token and send the email
            var passwordResetToken = Guid.NewGuid().ToString();

            // Update the UserAdministration with the password reset token
            var userAdmin = user.UserAdministration;
            userAdmin.PasswordResetToken = passwordResetToken;
            _context.UserAdministration.Update(userAdmin);
            await _context.SaveChangesAsync();

            var resetUrl = $"{Request.Scheme}://localhost:3000/setpassword/{passwordResetToken}";

            // Send the password set email
            var emailSent = await _emailService.SendEmailAsync(
                user.Email,
                "Set Your Password",
                $"Please set your password by clicking on the following link: <a href='{resetUrl}'>Set Password</a>"
            );

            if (!emailSent)
            {
                return StatusCode(500, "Failed to send the password set email.");
            }
        }

        // Now, create the session regardless of whether the password was set or not
        var sessionId = Guid.NewGuid().ToString();
        var session = new UserSession
        {
            UserSessionId = sessionId,
            UserId = user.UserId,
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            Data = "{}",
            IsActive = true
        };
        
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        // Set the session ID in a cookie
        CookieService.SetCookie(HttpContext, "SessionId", sessionId);
        bool isPasswordSet = !(string.IsNullOrEmpty(user.PasswordHash) || user.PasswordHash == "google-login");

        return Ok(new
        {
            success = true,
            message = "Google login successful!",
            UserSessionId = sessionId,
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            PasswordSet = isPasswordSet, // Add this field to indicate if the password is set

            Profile = new
            {
                user.UserProfile.FullName,
                user.UserProfile.PhoneNumber,
                user.UserProfile.StreetAddress,
                user.UserProfile.PostalCode
            }
        });
    }



    [HttpPost("verify-google-token")]
    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _clientId } // Ensure this matches your actual client ID
            });

            return payload; // This should contain the user details like email and name
        }
        catch (Exception ex)
        {
            // Log or throw specific error here
            throw new Exception("Invalid Google token", ex);
        }
    }

    [HttpPost("request-password-set")]
    public async Task<IActionResult> RequestPasswordSet()
    {
        var sessionId = HttpContext.Request.Cookies["SessionId"];

        if (string.IsNullOrEmpty(sessionId))
        {
            return Ok(new { sessionValid = false });
        }

        // Find the session by SessionId and ensure it's active
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

        if (session == null)
        {
            return BadRequest(new { Message = "Session is not valid or active." });
        }

        // Get the user based on the session information
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == session.UserId);

        if (user == null)
        {
            return BadRequest(new { Message = "User not found." });
        }

        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.User.Email == user.Email);

        if (userAdmin == null)
            return BadRequest(new { Message = "No user found with the provided email." });

        // Check if the password has already been set
        if (!string.IsNullOrEmpty(userAdmin.User.PasswordHash) && userAdmin.User.PasswordHash != "google-login")
        {
            return BadRequest(new { Message = "Password has already been set." });
        }

        // Generate password set token (similar to password reset token)
        userAdmin.PasswordResetToken = Guid.NewGuid().ToString(); // Reusing the reset token for setting the password

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();

        // Generate the set password URL
        var setPasswordUrl = $"{Request.Scheme}://localhost:3000/setpassword/{userAdmin.PasswordResetToken}";

        // Send the password set token via email
        var emailSent = await _emailService.SendEmailAsync(
            user.Email,
            "Set Your Password",
            $"Please set your password by clicking on the following link: <a href='{setPasswordUrl}'>Set Password</a>"
        );

        if (!emailSent)
            return StatusCode(500, "Failed to send the email.");

        return Ok(new { success = true, Message = "Password set token generated.", Token = userAdmin.PasswordResetToken });
    }



    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] PasswordSetTokenRequest request)
    {
        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.PasswordResetToken == request.Token);

        if (userAdmin == null || string.IsNullOrEmpty(userAdmin.PasswordResetToken))
            return BadRequest(new { message = "Invalid or expired token." });

        // Check if the password has already been set
        if (!string.IsNullOrEmpty(userAdmin.User.PasswordHash) && userAdmin.User.PasswordHash != "google-login")
        {
            return BadRequest(new { message = "Password has already been set." });
        }

        // Set the password and invalidate the token
        userAdmin.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        userAdmin.PasswordResetToken = null; // Nullify the token after use

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, Message = "Password set successfully." });
    }


    // Request Models
    public class PasswordSetTokenRequest
    {
        public string Token { get; set; }  // The password set token from the email
        public string NewPassword { get; set; }  // The new password to be set
    }

    public class GoogleUser
    {
        public string Email { get; set; }
        public string Name { get; set; }
    }
    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("Password", ErrorMessage = "Password and confirm password do not match.")]
        public string ConfirmPassword { get; set; }

        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? StreetAddress { get; set; }
        public string? PostalCode { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }

    public class PasswordResetRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
    }

    public class PasswordResetTokenRequest
    {
        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; }

        [Required(ErrorMessage = "New Password is required.")]
        public string NewPassword { get; set; }
    }

    public class EmailVerificationRequest
    {
        [Required(ErrorMessage = "Verification token is required.")]
        public string Token { get; set; }
    }

    public class VerificationEmailRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
    }
}
    