using Cold_Storage_GO.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionMiddleware> _logger;

        public SessionMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, ILogger<SessionMiddleware> logger)
        { 
            _next = next;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestPath = context.Request.Path.Value;
            var excludedPaths = new[]
            {
                "/api/Auth/login","/api/Auth/register","/api/Auth/request-password-reset","/api/Auth/reset-password","/api/Auth/staff/login", "/swagger", "/api/Account/profile/", "/api/HelpCentre",
                "/api/MealKit", "/api/Recipes", "/api/deliveries", "/api/deliveries/",
                "/api/subscriptions"
            };


            _logger.LogInformation("Request Path: {RequestPath}", requestPath);
            var sessionId = context.Request.Cookies["SessionId"];
            _logger.LogInformation("SessionId Cookie: {SessionId}", sessionId ?? "Not provided");

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

                var userSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == sessionId);

                if (userSession != null)
                {
                    _logger.LogInformation("Found user session for SessionId: {SessionId}", sessionId);

                    if (string.IsNullOrEmpty(userSession.Data))
                    {
                        userSession.Data = "[]";
                        _context.UserSessions.Update(userSession);
                        await _context.SaveChangesAsync();
                    }

                    if (!userSession.IsActive)
                    {
                        _logger.LogWarning("User session is inactive for SessionId: {SessionId}", sessionId);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        _context.UserSessions.Remove(userSession);
                        await _context.SaveChangesAsync();
                        await context.Response.WriteAsync("Invalid or expired user session.");
                        return;
                    }

                    var sessionExpiry = userSession.LastAccessed.AddMinutes(30);
                    if (DateTime.UtcNow > sessionExpiry)
                    {
                        _logger.LogWarning("User session expired for SessionId: {SessionId}", sessionId);
                        userSession.IsActive = false;
                        _context.UserSessions.Remove(userSession);
                        await _context.SaveChangesAsync();

                        CookieService.SetCookie(context, "SessionId", "", -1);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("User session expired and deleted.");
                        return;
                    }

                    userSession.LastAccessed = DateTime.UtcNow;
                    _context.UserSessions.Update(userSession);
                    await _context.SaveChangesAsync();

                    CookieService.SetCookie(context, "SessionId", userSession.UserSessionId);
                    await _next(context);
                    return;
                }

                var staffSession = await _context.StaffSessions.FirstOrDefaultAsync(s => s.StaffSessionId == sessionId);

                if (staffSession != null)
                {
                    _logger.LogInformation("Found staff session for SessionId: {SessionId}", sessionId);

                    if (!staffSession.IsActive)
                    {
                        _logger.LogWarning("Staff session is inactive for SessionId: {SessionId}", sessionId);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        _context.StaffSessions.Remove(staffSession);
                        await _context.SaveChangesAsync();
                        await context.Response.WriteAsync("Invalid or expired staff session.");
                        return;
                    }

                    var staffSessionExpiry = staffSession.LastAccessed.AddMinutes(30);
                    if (DateTime.UtcNow > staffSessionExpiry)
                    {
                        _logger.LogWarning("Staff session expired for SessionId: {SessionId}", sessionId);
                        staffSession.IsActive = false;
                        _context.StaffSessions.Remove(staffSession);
                        await _context.SaveChangesAsync();

                        CookieService.SetCookie(context, "SessionId", "", -1);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Staff session expired and deleted.");
                        return;
                    }

                    staffSession.LastAccessed = DateTime.UtcNow;
                    _context.StaffSessions.Update(staffSession);
                    await _context.SaveChangesAsync();

                    CookieService.SetCookie(context, "SessionId", staffSession.StaffSessionId);
                    await _next(context);
                    return;
                }

                _logger.LogWarning("No valid session found for SessionId: {SessionId}", sessionId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Session ID is invalid or does not exist.");
            }
        }
    }
}
