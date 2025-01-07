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
        public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryRequest request)
        {
            await _service.CreateDeliveryAsync(request.OrderId, request.DeliveryDatetime);
            return Ok("Delivery created successfully!");
        }

        [HttpPut("{deliveryId}")]
        public async Task<IActionResult> UpdateDeliveryStatus(Guid deliveryId, [FromBody] UpdateDeliveryStatusRequest request)
        {
            await _service.UpdateDeliveryStatusAsync(deliveryId, request.Status);
            return Ok("Delivery status updated successfully!");
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetDeliveriesByOrder(Guid orderId)
        {
            var deliveries = await _service.GetDeliveriesByOrderAsync(orderId);
            return Ok(deliveries);
        }
    }
}
