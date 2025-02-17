namespace Cold_Storage_GO.Models
{
    public class Notification
    {
        public Guid NotificationId { get; set; } // Unique identifier for the notification
        public Guid UserId { get; set; } // ID of the user receiving the notification
        public string Type { get; set; } // Type of notification (e.g., 'follow', 'comment', 'like', 'system')
        public string Title { get; set; } // Title of the notification
        public string Content { get; set; } // Detailed message of the notification
        public bool IsRead { get; set; } // Read status (false = unread, true = read)
        public DateTime CreatedAt { get; set; } // Creation date of the notification
    }
}
