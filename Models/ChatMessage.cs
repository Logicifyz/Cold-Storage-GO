using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class ChatMessage
    {
        [Key]
        public Guid MessageId { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }

        // To track which user this message is associated with
        public Guid UserId { get; set; }

        // To track which staff this message is associated with
        public Guid StaffId { get; set; }
        public Guid TicketId { get; set; }

        // If you want, you could also include a field for the sender type (user/staff)
        public bool IsStaffMessage { get; set; } // true if staff sent, false if user sent
    }

}
