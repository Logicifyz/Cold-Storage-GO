using Cold_Storage_GO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using SubscriptionServiceApp = Cold_Storage_GO.Services.SubscriptionService;


namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/stripe")]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly SubscriptionServiceApp _subscriptionService;
        private readonly ILogger<StripeController> _logger;  // ✅ Add the logger here

        public StripeController(
        IConfiguration config,
        SubscriptionServiceApp subscriptionService,
        ILogger<StripeController> logger) // ✅ Injecting the logger correctly
        {
            _config = config;
            _subscriptionService = subscriptionService;
            _logger = logger;  // ✅ Proper assignment of logger
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        [HttpPost("create-checkout-session")]
        public IActionResult CreateCheckoutSession([FromBody] CreateSubscriptionRequest request)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "sgd",
                    UnitAmount = (long)(request.Price * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = request.SubscriptionChoice
                    }
                },
                Quantity = 1
            }
        },
                Mode = "payment",
                SuccessUrl = "http://localhost:3000/subscription-success",
                CancelUrl = "http://localhost:3000/subscription-choice",

                // ✅ Fixed: Adding Metadata Properly
                Metadata = new Dictionary<string, string>
        {
            { "userId", request.UserId.ToString() },
            { "mealKitId", request.MealKitId.ToString() },
            { "frequency", request.Frequency.ToString() },
            { "deliveryTimeSlot", request.DeliveryTimeSlot },
            { "subscriptionType", request.SubscriptionType },
            { "subscriptionChoice", request.SubscriptionChoice },
            { "price", request.Price.ToString() }
        }
            };

            var service = new SessionService();
            var session = service.Create(options);
            _logger.LogInformation("✅ Checkout session created with metadata.");
            return Ok(new { id = session.Id });
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _config["Stripe:WebhookSecret"]
                );

                _logger.LogInformation("✅ Event Constructed: {EventType}", stripeEvent.Type);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    var metadata = session.Metadata;

                    // ✅ Debugging the Received Metadata
                    _logger.LogInformation("✅ Received Metadata: {Metadata}", metadata);

                    if (metadata == null || !metadata.Any())
                    {
                        _logger.LogError("❌ Metadata is empty. Verify Stripe setup.");
                        return BadRequest("❌ Metadata was empty. Ensure you are passing it correctly.");
                    }

                    if (!metadata.ContainsKey("userId"))
                    {
                        _logger.LogError("❌ Missing userId in metadata.");
                        return BadRequest("Missing userId in metadata.");
                    }

                    var userId = Guid.Parse(metadata["userId"]);
                    var mealKitId = Guid.Parse(metadata["mealKitId"]);
                    var frequency = int.Parse(metadata["frequency"]);
                    var deliveryTimeSlot = metadata["deliveryTimeSlot"];
                    var subscriptionType = metadata["subscriptionType"];
                    var subscriptionChoice = metadata["subscriptionChoice"];
                    var price = Convert.ToDecimal(metadata["price"]);

                    await _subscriptionService.CreateSubscriptionAsync(
                        userId, mealKitId, frequency, deliveryTimeSlot, subscriptionType, subscriptionChoice, price
                    );

                    _logger.LogInformation("✅ Subscription successfully created.");
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError("❌ Stripe Exception: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ General Exception: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

    }

}

