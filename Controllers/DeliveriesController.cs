using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Cold_Storage_GO.Services;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/deliveries")]
    public class DeliveriesController : ControllerBase
    {
        private readonly DeliveryService _service;

        public DeliveriesController(DeliveryService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDelivery(CreateDeliveryRequest request)
        {
            await _service.CreateDeliveryAsync(request.OrderId, request.DeliveryDatetime);
            return Ok("Delivery created successfully");
        }

        [HttpPut("{deliveryId}")]
        public async Task<IActionResult> UpdateDeliveryStatus(Guid deliveryId, [FromBody] UpdateDeliveryStatusRequest request)
        {
            await _service.UpdateDeliveryStatusAsync(deliveryId, request.Status);
            return Ok($"Delivery status updated to {request.Status}");
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetDeliveriesByOrder(Guid orderId)
        {
            var deliveries = await _service.GetDeliveriesByOrderAsync(orderId);
            if (!deliveries.Any()) return NotFound("No deliveries found for this order.");
            return Ok(deliveries);
        }

        // New: Staff GET for all deliveries
        [HttpGet("staff/all")]
        public async Task<IActionResult> GetAllDeliveries()
        {
            var deliveries = await _service.GetAllDeliveriesAsync();
            return Ok(deliveries);
        }

        [HttpGet("staff/status/{status}")]
        public async Task<IActionResult> GetDeliveriesByStatus(string status)
        {
            var deliveries = await _service.GetDeliveriesByStatusAsync(status);
            return Ok(deliveries);
        }

        [HttpGet("staff/search")]
        public async Task<IActionResult> SearchDeliveries([FromQuery] string query)
        {
            var deliveries = await _service.SearchDeliveriesAsync(query);
            return Ok(deliveries);
        }
    }
}
