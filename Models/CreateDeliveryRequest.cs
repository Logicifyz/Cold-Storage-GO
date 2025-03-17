namespace Cold_Storage_GO.Models;

public class CreateDeliveryRequest
{
    public Guid OrderId { get; set; }
    public DateTime DeliveryDatetime { get; set; }
}
