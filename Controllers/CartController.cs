using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.Text.Json;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly DbContexts _dbContext;

        public CartController(DbContexts dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] CartItemRequest request)
        {
            if (request.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }

            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is missing.");
            }

            var userSession = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId);

            if (userSession == null || !userSession.IsActive)
            {
                return Unauthorized("Invalid or expired session.");
            }

            try
            {
                // Initialize Data as an empty JSON array if null or empty
                if (string.IsNullOrEmpty(userSession.Data))
                {
                    userSession.Data = "[]";
                }

                // Deserialize existing cart data
                var cartItems = JsonSerializer.Deserialize<List<CartItem>>(userSession.Data);

                // Check if the item already exists
                var existingItem = cartItems.FirstOrDefault(ci => ci.MealKitId == request.MealKitId);
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity; // Update quantity
                }
                else
                {
                    cartItems.Add(new CartItem
                    {
                        MealKitId = request.MealKitId,
                        Quantity = request.Quantity,
                        Price = request.Price,
                    });
                }

                // Serialize and save back to session
                userSession.Data = JsonSerializer.Serialize(cartItems);
                userSession.LastAccessed = DateTime.UtcNow; // Update last accessed
                _dbContext.UserSessions.Update(userSession);
                await _dbContext.SaveChangesAsync();

                return Ok("Item added to cart.");
            }
            catch (JsonException)
            {
                // Handle corrupted JSON data
                userSession.Data = "[]"; // Reset to empty array
                userSession.LastAccessed = DateTime.UtcNow; // Update last accessed
                _dbContext.UserSessions.Update(userSession);
                await _dbContext.SaveChangesAsync();

                return BadRequest("Session data was corrupted and has been reset. Please try again.");
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error adding to cart: {ex.Message}");
                return StatusCode(500, "An error occurred while adding to the cart.");
            }
        }

        [HttpGet("view-cart")]
        public async Task<IActionResult> ViewCart()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is missing.");
            }

            var userSession = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
            {
                return Unauthorized("Invalid or expired session.");
            }

            var cartItems = string.IsNullOrEmpty(userSession.Data)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(userSession.Data);

            // Retrieve MealKit details
            var mealKitIds = cartItems.Select(ci => ci.MealKitId).ToList();
            var mealKitDetails = await _dbContext.MealKits
                .Where(mk => mealKitIds.Contains(mk.MealKitId))
                .ToListAsync();

            // Format the response
            var cartWithDetails = cartItems.Select(ci => new
            {
                ci.MealKitId,
                ci.Quantity,
                ci.Price,
                TotalPrice = ci.Quantity * ci.Price,
                MealKit = mealKitDetails.FirstOrDefault(mk => mk.MealKitId == ci.MealKitId) // Ensure MealKit is not null
            }).Where(ci => ci.MealKit != null);

            // Return the formatted response
            return Ok(cartWithDetails);
        }

        public class CartItemRequest
        {
            public Guid MealKitId { get; set; }
            public int Quantity { get; set; }
            public int Price {  get; set; }
        }

    }
}