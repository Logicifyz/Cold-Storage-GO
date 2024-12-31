using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
            var excludedPaths = new[] { "/api/Auth", "/swagger", "/api/Account/profile/" };

            // Skip session validation for excluded paths
            if (excludedPaths.Any(path => requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<DbContexts>();

                // Retrieve session ID from the request headers
                var sessionId = context.Request.Headers["SessionId"].ToString();
                if (string.IsNullOrEmpty(sessionId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Session ID is missing.");
                    return;
                }

                // Validate the session in the database
                var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
                if (session == null || !session.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    _context.Sessions.Remove(session);
                    await _context.SaveChangesAsync();
                    await context.Response.WriteAsync("Invalid or expired session.");
                    return;
                }

                // Check session expiry (e.g., 30 minutes of inactivity)
                var sessionExpiry = session.LastAccessed.AddMinutes(30);
                if (DateTime.UtcNow > sessionExpiry)
                {
                    session.IsActive = false;
                    _context.Sessions.Remove(session);
                    await _context.SaveChangesAsync();

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Session expired and deleted.");
                    return;
                }

                // Update LastAccessed timestamp
                session.LastAccessed = DateTime.UtcNow;
                _context.Sessions.Update(session);
                await _context.SaveChangesAsync();

                // Proceed to the next middleware or endpoint
                await _next(context);
            }
        }
    }
}
