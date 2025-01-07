using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services
{
    public class OrderService
    {
        private readonly DbContexts _context;

        public OrderService(DbContexts context)
        {
            _context = context;
        }

        // ✅ User Creates an Order
        public async Task CreateOrderAsync(Guid userId, Guid mealKitId, decimal totalPrice)
        {
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                UserId = userId,
                MealKitId = mealKitId,
                TotalPrice = totalPrice,
                OrderDate = DateTime.UtcNow,
                OrderStatus = "Confirmed"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        // ✅ Staff Updates Order Status
        public async Task UpdateOrderStatusAsync(Guid orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) throw new Exception("Order not found.");

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();
        }

        // ✅ Staff Management: Get All Orders
        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        // ✅ Staff Management: Get Orders by Status
        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
        {
            return await _context.Orders
                .Where(o => o.OrderStatus == status)
                .ToListAsync();
        }

        // ✅ Staff Management: Search Orders (Order ID or Customer ID)
        public async Task<IEnumerable<Order>> SearchOrdersAsync(string query)
        {
            return await _context.Orders
                .Where(o => o.OrderId.ToString().Contains(query) || o.UserId.ToString().Contains(query))
                .ToListAsync();
        }
    }
}
