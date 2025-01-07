using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffSupportController : ControllerBase
    {
        private readonly DbContexts _context;

        public StaffSupportController(DbContexts context)
        {
            _context = context;
        }

        // Consolidated method to validate staff session and role
        private async Task<IActionResult> ValidateStaffSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is required.");
            }

            // Find the active staff session
            var staffSession = await _context.StaffSessions
                .FirstOrDefaultAsync(ss => ss.StaffSessionId == sessionId && ss.IsActive);

            if (staffSession == null)
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            // Ensure that the associated user has the 'staff' role
            var staff = await _context.Staff
                .FirstOrDefaultAsync(u => u.StaffId == staffSession.StaffId && u.Role == "staff");

            if (staff == null)
            {
                return Unauthorized("User is not a staff member.");
            }

            return null; // Valid session and role
        }

        // Get all tickets with filters
        [HttpGet("tickets")]
        public async Task<IActionResult> GetAllTickets(
    [FromQuery] string status = null,
    [FromQuery] string priority = null,
    [FromQuery] string assignedTo = null,
    [FromQuery] string category = null,
    [FromQuery] string subject = null,
    [FromQuery] Guid? ticketId = null)  // Added ticketId filter
        {
            // Get the session ID from the request header
            var sessionId = Request.Headers["SessionId"].ToString();

            // Validate the staff session and role
            var validationResponse = await ValidateStaffSession(sessionId);
            if (validationResponse != null)
            {
                return validationResponse;
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
            // Get the session ID from the request header
            var sessionId = Request.Headers["SessionId"].ToString();

            // Validate the staff session and role
            var validationResponse = await ValidateStaffSession(sessionId);
            if (validationResponse != null)
            {
                return validationResponse;
            }

            var ticket = await _context.SupportTickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return NotFound("Ticket not found.");
            }

            // Update ticket properties with the request values
            ticket.Status = request.Status ?? ticket.Status;
            ticket.Priority = request.Priority ?? ticket.Priority;
            ticket.StaffId = request.AssignedTo ?? ticket.StaffId;
            ticket.ResolvedAt = request.ResolvedAt ?? ticket.ResolvedAt;

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
        public Guid? AssignedTo { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}