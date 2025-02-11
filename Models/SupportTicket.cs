using System;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class SupportTicket
    {
        [Key]
        public Guid TicketId { get; set; } = Guid.NewGuid(); // Auto-generate UUID

        [Required]
        public Guid UserId { get; set; } // Foreign key linking to the User model

        public Guid? StaffId { get; set; } // Foreign key linking to the Staff model (nullable for unassigned tickets)

        [Required]
        [StringLength(200)]
        public string Subject { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        [Required]
        public string Details { get; set; }

        [Required]
        [StringLength(20)]
        public string? Priority { get; set; } // Nullable priority to be set by staff later

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Unassigned"; // Default status

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Auto-set creation date

        public DateTime? ResolvedAt { get; set; } // Nullable for unresolved tickets
    }
}
