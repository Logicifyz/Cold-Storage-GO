namespace Cold_Storage_GO.Models
{
    public class StaffSession
    {
        public string StaffSessionId { get; set; } // Unique ID for the session
        public Guid StaffId { get; set; } // Reference to the staff member
        public DateTime CreatedAt { get; set; } // Session creation time
        public DateTime LastAccessed { get; set; } // Last accessed time
        public bool IsActive { get; set; } // Indicates whether the session is active
        public string Data { get; set; } // Additional session data (optional, could be JSON)

        // Navigation property for Staff
        //
    }
}
