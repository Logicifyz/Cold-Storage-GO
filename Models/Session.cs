namespace Cold_Storage_GO.Models
{
    public class Session
    {
 
            public Guid Id { get; set; } // Primary Key

            public string SessionId { get; set; } // Unique session identifier, e.g., ASP.NET_SessionId or custom AuthToken.

            public Guid UserId { get; set; } // Associated user ID.

            public DateTime CreatedAt { get; set; } // Timestamp for when the session was created.

            public DateTime LastAccessed { get; set; } // Timestamp for the last activity in the session.

            public string Data { get; set; } // Serialized session data (JSON or any format).

            public bool IsActive { get; set; } // Flag to indicate if the session is active.
        }

}

