namespace Cold_Storage_GO.Models
{
    public class CreateSubscriptionRequest
    {
        public Guid UserId { get; set; }
        public Guid MealKitId { get; set; }
        public int Frequency { get; set; } // Frequency in days
        public bool AutoRenewal { get; set; }
    }
}
