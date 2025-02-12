using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Cold_Storage_GO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Cold_Storage_GO.Middleware;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class SupportController : ControllerBase
    {
        private readonly DbContexts _context;

        public SupportController(DbContexts context)
        {
            _context = context;
        }

        [HttpPost("OpenTicket")]
        public async Task<IActionResult> OpenTicket([FromForm] OpenTicketRequest request)
        {
            // Retrieve UserSessionId from the session middleware
            var userSessionId = HttpContext.Request.Cookies["SessionId"];
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == userSessionId);

            if (session == null || !session.IsActive)
            {
                return Unauthorized("Invalid or expired session.");
            }

            // Create new support ticket
            var ticket = new SupportTicket
            {
                UserId = session.UserId,
                Subject = request.Subject,
                Category = request.Category,
                Details = request.Details,
                Priority = "Unassigned",
                Status = "Unassigned",
                CreatedAt = DateTime.UtcNow
            };
            var ticketEvent = new SupportTicketEvent
            {
                TicketId = ticket.TicketId,
                UserId = session.UserId,
                Subject = request.Subject,
                Category = request.Category,
                Priority = "Unassigned", // or your logic for priority
                Status = "Unassigned"
            };

            _context.SupportTicketEvents.Add(ticketEvent);
            // Add ticket to database
            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync(); // Save ticket first to generate TicketId

            // If images are provided, save them to the TicketImage table
            if (request.Images != null && request.Images.Any())
            {
                foreach (var image in request.Images)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        var ticketImage = new TicketImage
                        {
                            TicketId = ticket.TicketId,
                            ImageData = memoryStream.ToArray(),
                            UploadedAt = DateTime.UtcNow
                        };

                        _context.TicketImage.Add(ticketImage);
                    }
                }

                await _context.SaveChangesAsync(); // Save images to TicketImage table
            }

            return Ok(new { TicketId = ticket.TicketId, Message = "Ticket opened successfully." });
        }


        [HttpGet("GetTickets")]
        public async Task<IActionResult> GetTickets()
        {
            // Retrieve UserSessionId from the session using UserSessionId from headers
            var userSessionId = HttpContext.Request.Cookies["SessionId"];

            // Attempt to find the session by UserSessionId
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == userSessionId);

            // If session is invalid or expired, return Unauthorized response
            if (session == null || !session.IsActive)
            {
                return Unauthorized("Invalid or expired session.");
            }

            // Retrieve all tickets for the user associated with the session UserId
            var tickets = await _context.SupportTickets
                .Where(t => t.UserId == session.UserId) // Filter tickets by UserId from session
                .Include(t => t.Images) // Include related images
                .ToListAsync();

            // If no tickets are found, return a message
            if (!tickets.Any())
            {
                return NotFound("No tickets found for this user.");
            }

            // Iterate through the tickets and handle possible NULL values in the response
            var ticketResponses = tickets.Select(ticket =>
            {
                // Include ticket information
                var response = new
                {
                    ticket.TicketId,
                    ticket.UserId,
                    ticket.Subject,
                    ticket.Category,
                    ticket.Details,
                    Priority = ticket.Priority ?? "Unassigned",
                    Status = ticket.Status ?? "Unassigned",
                    CreatedAt = ticket.CreatedAt,
                    ResolvedAt = ticket.ResolvedAt, // Always included, null if unresolved
                    Images = ticket.Images.Select(img => new
                    {
                        img.ImageId,
                        img.UploadedAt,
                        ImageData = img.ImageData.Length > 0 ? Convert.ToBase64String(img.ImageData) : null // Optionally convert to base64 string
                    }).ToList() // Include images as base64 (optional, you can change this to store a path instead)
                };

                

                return response;
            }).ToList();

            return Ok(ticketResponses);
        }


        // Request model for opening a ticket
        public class OpenTicketRequest
        {
            public string Subject { get; set; }
            public string Category { get; set; }
            public string Details { get; set; }
            public IFormFile[]? Images { get; set; } // Optional image array

        }
    }
}
