using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services;

public class DeliveryService
{
    private readonly DbContexts _context;

    public DeliveryService(DbContexts context)
    {
        _context = context;
    }

    public async Task CreateDeliveryAsync(Guid orderId, DateTime deliveryDatetime)
    {
        var delivery = new Delivery
        {
            DeliveryId = Guid.NewGuid(),
            OrderId = orderId,
            DeliveryDatetime = deliveryDatetime,
            DeliveryStatus = "Pending"
        };

        _context.Deliveries.Add(delivery);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDeliveryStatusAsync(Guid deliveryId, string status)
    {
        var delivery = await _context.Deliveries.FirstOrDefaultAsync(d => d.DeliveryId == deliveryId);
        if (delivery == null) throw new Exception("Delivery not found.");

        delivery.DeliveryStatus = status;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Delivery>> GetDeliveriesByOrderAsync(Guid orderId)
    {
        return await _context.Deliveries
            .Where(d => d.OrderId == orderId)
            .ToListAsync();
    }
}
