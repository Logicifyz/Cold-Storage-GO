namespace Cold_Storage_GO.Models;

public class Subscription
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public int Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool? AutoRenewal { get; set; }  // ✅ Nullable now
    public string DeliveryTimeSlot { get; set; }  
    public string SubscriptionType { get; set; }  
    public bool? IsFrozen { get; set; }  // ✅ Nullable now
    public string? StripeSessionId { get; set; } // ✅ Nullable now
    public string SubscriptionChoice { get; set; }  
    public decimal Price { get; set; }
    public string Status { get; set; }  // Active, Canceled, Expired
    public User User { get; set; }
    public DateTime? ScheduledFreezeStartDate { get; set; }
    public DateTime? ScheduledFreezeEndDate { get; set; }
    public bool IsScheduledForFreeze { get; set; } = false;
}

