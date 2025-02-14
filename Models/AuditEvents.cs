namespace Cold_Storage_GO.Models
{
    public class CartEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid MealKitId { get; set; }
        public int Quantity { get; set; }
        public DateTime EventTime { get; set; } = DateTime.UtcNow;
    }

    public class OrderEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime EventTime { get; set; } = DateTime.UtcNow;
        public int ItemCount { get; set; }
    }
    public class RewardRedemptionEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Unique identifier for the analytic event record.
        public Guid RedemptionEventId { get; set; } = Guid.NewGuid();

        // Identifier linking back to the Redemption record.
        public Guid RedemptionId { get; set; }

        // The user who redeemed the reward.
        public Guid UserId { get; set; }

        // The reward that was redeemed.
        public Guid RewardId { get; set; }

        // When the reward was redeemed.
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

        // Expiry date for the redeemed reward.
        public DateTime ExpiryDate { get; set; }

        // Whether the reward is still usable.
        public bool RewardUsable { get; set; }
    }
    public class SupportTicketEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Unique identifier for the analytic event record.
        public Guid TicketEventId { get; set; } = Guid.NewGuid();

        // The support ticket that was created.
        public Guid TicketId { get; set; }

        // The user who opened the ticket.
        public Guid UserId { get; set; }

        // Brief subject of the ticket.
        public string Subject { get; set; }

        // The category of the ticket.
        public string Category { get; set; }

        // The priority level of the ticket.
        public string Priority { get; set; }

        // The status of the ticket at creation (or later updates).
        public string Status { get; set; }

        // When the ticket event was recorded.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SubscriptionEvent

    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Unique identifier for the analytic event record.
        public Guid SubscriptionEventId { get; set; } = Guid.NewGuid();

        // The subscription this event is related to.
        public Guid SubscriptionId { get; set; }

        // The user associated with the subscription.
        public Guid UserId { get; set; }

        // The type of event – for example "Created", "Canceled", "Frozen", "Unfrozen", etc.
        public string EventType { get; set; }

        // When the event occurred.
        public DateTime EventTime { get; set; } = DateTime.UtcNow;

        // Any additional details or context about the event.
        public string Details { get; set; }
    }
}
