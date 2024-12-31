using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
            // Define paths to exclude from session validation
            var excludedPaths = new[] { "/api/Auth", "/swagger" }; // Add login and register endpoints
            var requestPath = context.Request.Path.Value;

            // Skip session validation for excluded paths
            if (excludedPaths.Any(path => requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context); // Proceed to the next middleware or endpoint
                return;
            }

            // Create a scope to resolve DbContexts
            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DbContexts>();

                // Retrieve session ID from the request headers
                var sessionId = context.Request.Headers["SessionId"].ToString();
                if (string.IsNullOrEmpty(sessionId))
                {
                    // Return Unauthorized if SessionId is missing
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Session ID is missing.");
                    return;
                }

                // Validate the session in the database
                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
                if (session == null)
                {
                    // Return Unauthorized if session is invalid or expired
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid or expired session.");
                    return;
                }

                // Update the session's LastAccessed time
                session.LastAccessed = DateTime.UtcNow;
                _context.Sessions.Update(session);
                await _context.SaveChangesAsync();

                // Proceed with the next middleware or endpoint
                await _next(context);
            }
        }


        // Optional: Expiry Check for a specific session (you can expose this as a separate endpoint if needed)
        public async Task<IActionResult> CheckSessionExpiration(string sessionId)
        {
            // Create a scope to resolve DbContexts
            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DbContexts>();

                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    return new UnauthorizedResult(); // Unauthorized response for missing session
                }

                // Check for expiry (e.g., 30 minutes of inactivity)
                var sessionExpiry = session.LastAccessed.AddMinutes(30);
                if (DateTime.UtcNow > sessionExpiry)
                {
                    session.IsActive = false;
                    _context.Sessions.Update(session);
                    await _context.SaveChangesAsync();
                    return new UnauthorizedResult(); // Unauthorized response for expired session
                }

                return new OkResult(); // Return Ok if the session is still valid
            }
        }
    }
}
