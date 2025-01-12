namespace Cold_Storage_GO.Models;

public class Subscription
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public Guid MealKitId { get; set; }
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
}

