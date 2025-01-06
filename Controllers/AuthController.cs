using Cold_Storage_GO;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DbContexts _context;
    private readonly EmailService _emailService;

    public AuthController(DbContexts context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if the model is valid
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return validation errors
        }

        // Check if email is already taken
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest("Email is already in use.");
        }

        // Check if username is already taken
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest("Username is already taken.");
        }

        // Password and Confirm Password do not match
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest("Password and confirm password do not match.");
        }

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

        // Send verification email
        var emailSent = await _emailService.SendEmailAsync(
            request.Email,
            "Email Verification",
            $"Please verify your email using the following token: {userAdmin.VerificationToken}"
        );

        if (!emailSent)
            return StatusCode(500, "Failed to send verification email.");

        return Ok(new { Message = "Registration successful. Please check your email for verification.", wallet });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
        .Include(u => u.UserProfile)
        .Include(u => u.UserAdministration)
        .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var userAdmin = user.UserAdministration;

        // Check if account is locked
        if (userAdmin.LockoutUntil.HasValue && userAdmin.LockoutUntil > DateTime.UtcNow)
        {
            return StatusCode(403, $"Account is locked until {userAdmin.LockoutUntil.Value:u}");
        }

        // Reset failed login attempts if it has been more than 10 minutes since the last failed attempt
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
                userAdmin.LockoutUntil = DateTime.UtcNow.AddMinutes(5); // Lock for 15 minutes
            }

            await _context.SaveChangesAsync();
            return Unauthorized("Invalid email or password.");
        }


        userAdmin.FailedLoginAttempts = 0;
        userAdmin.LockoutUntil = null; // Clear lockout time
        userAdmin.LastFailedLogin = null; // Clear the last failed login time

        await _context.SaveChangesAsync();
        // Check for an existing active session
        var existingSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserId == user.UserId && s.IsActive);

        if (existingSession != null)
        {
            // Update LastAccessed for the existing session
            existingSession.LastAccessed = DateTime.UtcNow;
            _context.UserSessions.Update(existingSession);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                UserSessionId = existingSession.UserSessionId,
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

        // Create a new session if none exists
        var UserSessionId = Guid.NewGuid().ToString();
        var session = new UserSession
        {
            UserSessionId = UserSessionId,
            UserId = user.UserId,
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            Data = "{}",
            IsActive = true
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            UserSessionId = UserSessionId,
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
            return BadRequest("No user found with the provided email.");

        // Generate password reset token
        userAdmin.PasswordResetToken = Guid.NewGuid().ToString();
        // LockoutUntil removed, it was previously set for token expiration, no longer required.

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();

        // Send the password reset token via email
        var emailSent = await _emailService.SendEmailAsync(
            request.Email,
            "Password Reset Request",
            $"Your password reset token is: {userAdmin.PasswordResetToken}"
        );

        if (!emailSent)
            return StatusCode(500, "Failed to send the email.");

        return Ok(new { Message = "Password reset token generated.", Token = userAdmin.PasswordResetToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetTokenRequest request)
    {
        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.PasswordResetToken == request.Token);

        if (userAdmin == null || string.IsNullOrEmpty(userAdmin.PasswordResetToken))
            return BadRequest("Invalid or expired token.");

        // Reset the password and invalidate the reset token
        userAdmin.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        userAdmin.PasswordResetToken = null; // Nullify the token after use

        _context.UserAdministration.Update(userAdmin);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Password reset successful." });
    }

    // Endpoint for email verification
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
    {
        var userAdmin = await _context.UserAdministration
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.VerificationToken == request.Token);

        if (userAdmin == null)
            return BadRequest("Invalid or expired verification token.");

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
        // Check if the user is logged in by verifying their session
        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == request.UserSessionId && s.IsActive);

        if (session == null)
        {
            return Unauthorized("User is not logged in or session has expired.");
        }

        // Retrieve the user related to the session
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
        var emailSent = await _emailService.SendEmailAsync(
            request.Email,
            "Email Verification",
            $"Please verify your email using the following token: {userAdmin.VerificationToken}"
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
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Email == request.Email);

        if (staff == null || staff.Role != "staff" || staff.Password != request.Password)
        {
            return Unauthorized("Invalid email, password, or role.");
        }

        // Check for an existing active session for the staff member
        var existingSession = await _context.StaffSessions
            .FirstOrDefaultAsync(s => s.StaffId == staff.StaffId && s.IsActive);

        if (existingSession != null)
        {
            // Update LastAccessed for the existing session
            existingSession.LastAccessed = DateTime.UtcNow;
            _context.StaffSessions.Update(existingSession);
            await _context.SaveChangesAsync();

            // Return the existing session details
            return Ok(new
            {
                SessionId = existingSession.StaffSessionId,
                StaffId = staff.StaffId,
                Name = staff.Name,
                Email = staff.Email,
                Role = staff.Role
            });
        }

        // If no active session exists, create a new one
        var staffSession = new StaffSession
        {
            StaffSessionId = Guid.NewGuid().ToString(),
            StaffId = staff.StaffId,
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            Data = "{}",
            IsActive = true
        };

        _context.StaffSessions.Add(staffSession);
        await _context.SaveChangesAsync();

        // Return a response with the new session and staff info
        return Ok(new
        {
            SessionId = staffSession.StaffSessionId,
            StaffId = staff.StaffId,
            Name = staff.Name,
            Email = staff.Email,
            Role = staff.Role
        });
    }



    // Request Models
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

        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string StreetAddress { get; set; }
        public string PostalCode { get; set; }
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

        [Required(ErrorMessage = "Session ID is required.")]
        public string UserSessionId { get; set; }
    }
}
    