namespace Cold_Storage_GO.Models
{
    public class CreateOrderRequest
    {
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; }
        public Guid MealKitId { get; set; }
        public int TotalPrice { get; set; }
        public string PromotionCode { get; set; }
        public string OrderNotes { get; set; }
    }
}
