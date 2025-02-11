using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class StaffSupportController : ControllerBase
    {
        private readonly DbContexts _context;

        public StaffSupportController(DbContexts context)
        {
            _context = context;
        }

        // Get all tickets with filters
        [HttpGet("tickets")]
        public async Task<IActionResult> GetAllTickets(
            [FromQuery] string status = null,
            [FromQuery] string priority = null,
            [FromQuery] string assignedTo = null,
            [FromQuery] string category = null,
            [FromQuery] string subject = null,
            [FromQuery] Guid? ticketId = null) // Added ticketId filter
        {
            // Get the session ID from the cookies
            var sessionId = Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is required.");
            }

            // Optional: If you still want to validate session, you can use the sessionId here
            var staffSession = await _context.StaffSessions
                .FirstOrDefaultAsync(ss => ss.StaffSessionId == sessionId && ss.IsActive);

            if (staffSession == null)
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            var query = _context.SupportTickets.AsQueryable();

            // Apply filters if they are provided
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            if (!string.IsNullOrEmpty(assignedTo))
            {
                query = query.Where(t => t.StaffId != null && t.StaffId.ToString() == assignedTo);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category == category);
            }

            if (!string.IsNullOrEmpty(subject))
            {
                query = query.Where(t => t.Subject.Contains(subject));
            }

            // Apply the ticketId filter if provided
            if (ticketId.HasValue)
            {
                query = query.Where(t => t.TicketId == ticketId.Value);
            }

            var tickets = await query.ToListAsync();
            return Ok(tickets);
        }

        // Update ticket
        [HttpPut("tickets/{ticketId}")]
        public async Task<IActionResult> UpdateTicket(Guid ticketId, [FromBody] UpdateTicketRequest request)
        {
            // Get the session ID from the cookies
            var sessionId = Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is required.");
            }

            // Get the active staff session
            var staffSession = await _context.StaffSessions
                .FirstOrDefaultAsync(ss => ss.StaffSessionId == sessionId && ss.IsActive);

            if (staffSession == null)
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            // Retrieve the ticket to be updated
            var ticket = await _context.SupportTickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            // Update ticket properties with the request values
            ticket.Status = request.Status ?? ticket.Status;
            ticket.Priority = request.Priority ?? ticket.Priority;

            // Assign the ticket to the current staff member based on their session
            ticket.StaffId = staffSession.StaffId; // Set to the staff ID from the session
            ticket.ResolvedAt = request.ResolvedAt ?? ticket.ResolvedAt;

            // Save the updated ticket
            _context.SupportTickets.Update(ticket);
            await _context.SaveChangesAsync();

            return Ok(ticket);
        }

    }

    // UpdateTicketRequest model to be used in the UpdateTicket method
    public class UpdateTicketRequest
    {
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
