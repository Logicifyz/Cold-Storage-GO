using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class UserSession
    {
 
            public Guid Id { get; set; } // Primary Key

            public string UserSessionId { get; set; } // Unique session identifier, e.g., ASP.NET_SessionId or custom AuthToken.

            public Guid UserId { get; set; } // Associated user ID.

            public DateTime CreatedAt { get; set; } // Timestamp for when the session was created.

            public DateTime LastAccessed { get; set; } // Timestamp for the last activity in the session.

            public bool IsActive { get; set; } // Flag to indicate if the session is active.
            public string Data { get; set; } // Serialized cart items

            // Add this property to store cart items
            [NotMapped]
            public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        public Guid MealKitId { get; set; }
        public int Quantity { get; set; }
    }

}

