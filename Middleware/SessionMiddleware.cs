using Cold_Storage_GO.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;  // Add this import for logging
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionMiddleware> _logger;  // Inject the logger

        public SessionMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, ILogger<SessionMiddleware> logger)
        {
            _next = next;
            _scopeFactory = scopeFactory;
            _logger = logger;  // Assign the injected logger
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestPath = context.Request.Path.Value;
            var excludedPaths = new[] { "/api/Auth", "/swagger", "/api/Account/profile/", "/api/HelpCentre", "/api/MealKit", "/api/Recipes", "/api/deliveries", "/api/deliveries/", "/api/subscriptions", "/api/subscriptions/", "/api/orders", "/api/orders/" };

            // Log the request path and session ID for debugging
            _logger.LogInformation("Request Path: {RequestPath}", requestPath);
            var sessionId = context.Request.Cookies["SessionId"];
            _logger.LogInformation("SessionId Cookie: {SessionId}", sessionId ?? "Not provided");

            // Skip session validation for excluded paths
            if (excludedPaths.Any(path => requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DbContexts>();

                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogWarning("Session ID is missing for request path: {RequestPath}", requestPath);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Session ID is missing!!");
                    return;
                }

                // First, check in UserSessions table
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId);

                if (userSession != null)
                {
                    _logger.LogInformation("Found user session for SessionId: {SessionId}", sessionId);

                    // Initialize the Data field if it's null or empty
                    if (string.IsNullOrEmpty(userSession.Data))
                    {
                        userSession.Data = "[]"; // Initialize with an empty JSON array
                        _context.UserSessions.Update(userSession);
                        await _context.SaveChangesAsync();
                    }

                    // Validate the User session
                    if (!userSession.IsActive)
                    {
                        _logger.LogWarning("User session is inactive for SessionId: {SessionId}", sessionId);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        _context.UserSessions.Remove(userSession);
                        await _context.SaveChangesAsync();
                        await context.Response.WriteAsync("Invalid or expired user session.");
                        return;
                    }

                    // Check session expiry (e.g., 30 minutes of inactivity)
                    var sessionExpiry = userSession.LastAccessed.AddMinutes(30);
                    if (DateTime.UtcNow > sessionExpiry)
                    {
                        _logger.LogWarning("User session expired for SessionId: {SessionId}", sessionId);
                        userSession.IsActive = false;
                        _context.UserSessions.Remove(userSession);
                        await _context.SaveChangesAsync();

                        // Clear the session cookie as it is expired
                        context.Response.Cookies.Append("SessionId", "", new CookieOptions
                        {
                            Expires = DateTime.UtcNow.AddMinutes(-1)
                        });

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("User session expired and deleted.");
                        return;
                    }

                    // Update LastAccessed timestamp for User session
                    userSession.LastAccessed = DateTime.UtcNow;
                    _context.UserSessions.Update(userSession);
                    await _context.SaveChangesAsync();

                    // Optionally, refresh the session cookie expiry
                    context.Response.Cookies.Append("SessionId", userSession.UserSessionId, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddMinutes(30) // Refresh expiry
                    });

                    await _next(context);
                    return;
                }

                // Next, check in StaffSessions table if no valid user session was found
                var staffSession = await _context.StaffSessions
                    .FirstOrDefaultAsync(s => s.StaffSessionId == sessionId);

                if (staffSession != null)
                {
                    _logger.LogInformation("Found staff session for SessionId: {SessionId}", sessionId);

                    // Validate the Staff session
                    if (!staffSession.IsActive)
                    {
                        _logger.LogWarning("Staff session is inactive for SessionId: {SessionId}", sessionId);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        _context.StaffSessions.Remove(staffSession);
                        await _context.SaveChangesAsync();
                        await context.Response.WriteAsync("Invalid or expired staff session.");
                        return;
                    }

                    // Check session expiry (e.g., 30 minutes of inactivity)
                    var staffSessionExpiry = staffSession.LastAccessed.AddMinutes(30);
                    if (DateTime.UtcNow > staffSessionExpiry)
                    {
                        _logger.LogWarning("Staff session expired for SessionId: {SessionId}", sessionId);
                        staffSession.IsActive = false;
                        _context.StaffSessions.Remove(staffSession);
                        await _context.SaveChangesAsync();

                        // Clear the session cookie for staff
                        context.Response.Cookies.Append("SessionId", "", new CookieOptions
                        {
                            Expires = DateTime.UtcNow.AddMinutes(-1)
                        });

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Staff session expired and deleted.");
                        return;
                    }

                    // Update LastAccessed timestamp for Staff session
                    staffSession.LastAccessed = DateTime.UtcNow;
                    _context.StaffSessions.Update(staffSession);
                    await _context.SaveChangesAsync();

                    // Optionally, refresh the session cookie expiry
                    context.Response.Cookies.Append("SessionId", staffSession.StaffSessionId, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTime.UtcNow.AddMinutes(30)
                    });

                    await _next(context);
                    return;
                }

                // If no valid session found in either UserSessions or StaffSessions
                _logger.LogWarning("No valid session found for SessionId: {SessionId}", sessionId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Session ID is invalid or does not exist.");
            }
        }
    }
}
