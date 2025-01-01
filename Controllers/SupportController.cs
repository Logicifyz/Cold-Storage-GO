using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Cold_Storage_GO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Cold_Storage_GO.Middleware;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupportController : ControllerBase
    {
        private readonly DbContexts _context;

        public SupportController(DbContexts context)
        {
            _context = context;
        }

        // Open Ticket API
        [HttpPost("OpenTicket")]
        public async Task<IActionResult> OpenTicket([FromBody] OpenTicketRequest request)
        {
            // Retrieve UserSessionId from the session middleware
            var userSessionId = Request.Headers["SessionId"].ToString(); // Changed to UserSessionId
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == userSessionId); // Use UserSessionId

            if (session == null || !session.IsActive)
            {
                return Unauthorized("Invalid or expired session.");
            }

            // Create new support ticket
            var ticket = new SupportTicket
            {
                UserId = session.UserId, // Assuming the session has a reference to UserId
                Subject = request.Subject,
                Category = request.Category,
                Details = request.Details,
                Priority = "Unassigned", // Set Priority to null
                Status = "Unassigned", // Set Status to "Unassigned"
                CreatedAt = DateTime.UtcNow
            };

            // Add ticket to database
            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { TicketId = ticket.TicketId, Message = "Ticket opened successfully." });
        }

        [HttpGet("GetTickets")]
        public async Task<IActionResult> GetTickets()
        {
            // Retrieve UserSessionId from the session using UserSessionId from headers
            var userSessionId = Request.Headers["SessionId"].ToString(); // Changed to UserSessionId

            // Attempt to find the session by UserSessionId
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == userSessionId); // Use UserSessionId

            // If session is invalid or expired, return Unauthorized response
            if (session == null || !session.IsActive)
            {
                return Unauthorized("Invalid or expired session.");
            }

            // Retrieve all tickets for the user associated with the session UserId
            var tickets = await _context.SupportTickets
                .Where(t => t.UserId == session.UserId) // Filter tickets by UserId from session
                .ToListAsync();

            // If no tickets are found, return a message
            if (!tickets.Any())
            {
                return NotFound("No tickets found for this user.");
            }

            // Iterate through the tickets and handle possible NULL values in the response
            var ticketResponses = tickets.Select(ticket =>
            {
                var response = new
                {
                    ticket.TicketId,
                    ticket.UserId,
                    Subject = ticket.Subject,
                    Category = ticket.Category,
                    Details = ticket.Details,
                    Priority = ticket.Priority ?? "Unassigned",
                    Status = ticket.Status ?? "Unassigned",
                    CreatedAt = ticket.CreatedAt
                };

                // Dynamically add ResolvedAt if it's not null
                if (ticket.ResolvedAt.HasValue)
                {
                    response.GetType().GetProperty("ResolvedAt").SetValue(response, ticket.ResolvedAt);
                }

                return response;
            }).ToList();

            return Ok(ticketResponses);
        }
    }

    // Request model for opening a ticket
    public class OpenTicketRequest
    {
        public string Subject { get; set; }
        public string Category { get; set; }
        public string Details { get; set; }
    }
}
