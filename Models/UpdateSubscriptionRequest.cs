namespace Cold_Storage_GO.Models;

public class UpdateSubscriptionRequest
{
    public bool? AutoRenewal { get; set; } // Optional (Nullable)
    public bool? IsFrozen { get; set; }    // Optional (Nullable)
}
