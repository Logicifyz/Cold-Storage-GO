namespace Cold_Storage_GO.Models;

public class CreateSubscriptionRequest
{
    public Guid UserId { get; set; }
    public Guid MealKitId { get; set; }
    public int Frequency { get; set; } // Frequency in days
    public string DeliveryTimeSlot { get; set; } // Fixed error
    public string SubscriptionType { get; set; } // Fixed error (Monthly/Weekly)
    public string SubscriptionChoice { get; set; }  // New field added here
    public decimal Price { get; set; }
}

