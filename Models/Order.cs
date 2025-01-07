namespace Cold_Storage_GO.Models;

public class Order
{
    public Guid OrderId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public Guid MealKitId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; } // Changed to decimal to avoid casting error
    public string PromotionCode { get; set; }
    public string OrderNotes { get; set; }
    public string OrderStatus { get; set; }  // Fixed error by adding OrderStatus property
}
