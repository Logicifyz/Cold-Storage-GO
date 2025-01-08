namespace Cold_Storage_GO.Models
{
    public class Delivery
    {
        public Guid DeliveryId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime DeliveryDatetime { get; set; }
        public DateTime? ConfirmedDeliveryDatetime { get; set; }
        public string DeliveryStatus { get; set; }
    }
}
