namespace Cold_Storage_GO.Models
{
    public class Subscription
    {
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; }
        public Guid MealKitId { get; set; }
        public int Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenewal { get; set; }
    }
}
