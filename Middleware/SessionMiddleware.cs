using Cold_Storage_GO.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

        public SessionMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestPath = context.Request.Path.Value;
            var excludedPaths = new[] { "/api/Auth", "/swagger", "/api/Account/profile/", "/api/HelpCentre", "/api/MealKit", "/api/Recipes" };

            // Skip session validation for excluded paths
            if (excludedPaths.Any(path => requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DbContexts>();

                // Retrieve SessionId from the request headers
                var sessionId = context.Request.Headers["SessionId"].ToString();

                if (string.IsNullOrEmpty(sessionId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Session ID is missing!!");
                    return;
                }

                // First, check in UserSessions table
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId);

                if (userSession != null)
                {
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
                        userSession.IsActive = false;
                        _context.UserSessions.Remove(userSession);
                        await _context.SaveChangesAsync();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("User session expired and deleted.");
                        return;
                    }

                    // Update LastAccessed timestamp for User session
                    userSession.LastAccessed = DateTime.UtcNow;
                    _context.UserSessions.Update(userSession);
                    await _context.SaveChangesAsync();
                    await _next(context);
                    return;
                }

                // Next, check in StaffSessions table if no valid user session was found
                var staffSession = await _context.StaffSessions
                    .FirstOrDefaultAsync(s => s.StaffSessionId == sessionId);

                if (staffSession != null)
                {
                    // Validate the Staff session
                    if (!staffSession.IsActive)
                    {
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
                        staffSession.IsActive = false;
                        _context.StaffSessions.Remove(staffSession);
                        await _context.SaveChangesAsync();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Staff session expired and deleted.");
                        return;
                    }

                    // Update LastAccessed timestamp for Staff session
                    staffSession.LastAccessed = DateTime.UtcNow;
                    _context.StaffSessions.Update(staffSession);
                    await _context.SaveChangesAsync();
                    await _next(context);
                    return;
                }

                // If no valid session found in either UserSessions or StaffSessions
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Session ID is invalid or does not exist.");
            }
        }
    }
}
