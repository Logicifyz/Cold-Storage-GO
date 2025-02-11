using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class TicketImage
    {
        [Key]
        public Guid ImageId { get; set; } = Guid.NewGuid(); // Auto-generate UUID

        [Required]
        public Guid TicketId { get; set; } // Foreign key to SupportTicket

        [Required]
        public byte[] ImageData { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow; // Auto-set upload timestamp

        // Foreign key relationship
        [ForeignKey("TicketId")]
        public virtual SupportTicket SupportTicket { get; set; }
    }
}
