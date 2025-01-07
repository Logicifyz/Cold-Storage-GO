using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services
{
    public class DeliveryService
    {
        private readonly DbContexts _context;

        public DeliveryService(DbContexts context)
        {
            _context = context;
        }

        // ✅ User Creates a Delivery
        public async Task CreateDeliveryAsync(Guid orderId, DateTime deliveryDatetime)
        {
            var delivery = new Delivery
            {
                DeliveryId = Guid.NewGuid(),
                OrderId = orderId,
                DeliveryDatetime = deliveryDatetime,
                DeliveryStatus = "Order Confirmed"
            };

            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();
        }

        // ✅ Staff Updates Delivery Status
        public async Task UpdateDeliveryStatusAsync(Guid deliveryId, string status)
        {
            var delivery = await _context.Deliveries.FirstOrDefaultAsync(d => d.DeliveryId == deliveryId);
            if (delivery == null) throw new Exception("Delivery not found.");

            delivery.DeliveryStatus = status;
            await _context.SaveChangesAsync();
        }

        // ✅ User Checks Delivery by Order ID
        public async Task<IEnumerable<Delivery>> GetDeliveriesByOrderAsync(Guid orderId)
        {
            return await _context.Deliveries
                .Where(d => d.OrderId == orderId)
                .ToListAsync();
        }

        // ✅ Staff Management: Get All Deliveries
        public async Task<IEnumerable<Delivery>> GetAllDeliveriesAsync()
        {
            return await _context.Deliveries.ToListAsync();
        }

        // ✅ Staff Management: Get Deliveries by Status
        public async Task<IEnumerable<Delivery>> GetDeliveriesByStatusAsync(string status)
        {
            return await _context.Deliveries
                .Where(d => d.DeliveryStatus == status)
                .ToListAsync();
        }

        // ✅ Staff Management: Search Deliveries (by Order ID or Customer ID)
        public async Task<IEnumerable<Delivery>> SearchDeliveriesAsync(string query)
        {
            return await _context.Deliveries
                .Where(d => d.OrderId.ToString().Contains(query))
                .ToListAsync();
        }
    }
}
