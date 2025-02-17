using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
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
                var cartEvent = new CartEvent
                {
                    UserId = userSession.UserId,
                    MealKitId = request.MealKitId,
                    Quantity = request.Quantity
                };
                _dbContext.CartEvents.Add(cartEvent);

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
                MealKit = mealKitDetails.FirstOrDefault(mk => mk.MealKitId == ci.MealKitId)
            }).Where(ci => ci.MealKit != null);

            return Ok(cartWithDetails);
        }

        // NEW: DELETE endpoint to remove a cart item by MealKitId.
        [HttpDelete("{mealKitId}")]
        public async Task<IActionResult> RemoveFromCart(Guid mealKitId)
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

            try
            {
                if (string.IsNullOrEmpty(userSession.Data))
                {
                    return NotFound("Cart is empty.");
                }

                var cartItems = JsonSerializer.Deserialize<List<CartItem>>(userSession.Data);
                var itemToRemove = cartItems.FirstOrDefault(ci => ci.MealKitId == mealKitId);
                if (itemToRemove == null)
                {
                    return NotFound("Item not found in cart.");
                }

                cartItems.Remove(itemToRemove);
                userSession.Data = JsonSerializer.Serialize(cartItems);
                userSession.LastAccessed = DateTime.UtcNow;
                _dbContext.UserSessions.Update(userSession);
                await _dbContext.SaveChangesAsync();

                return Ok("Item removed from cart.");
            }
            catch (JsonException)
            {
                userSession.Data = "[]";
                userSession.LastAccessed = DateTime.UtcNow;
                _dbContext.UserSessions.Update(userSession);
                await _dbContext.SaveChangesAsync();
                return BadRequest("Session data was corrupted and has been reset. Please try again.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing cart item: {ex.Message}");
                return StatusCode(500, "An error occurred while removing the cart item.");
            }
        }

        public class CartItemRequest
        {
            [JsonPropertyName("mealKitId")]
            public Guid MealKitId { get; set; }

            [JsonPropertyName("quantity")]
            public int Quantity { get; set; }

            [JsonPropertyName("price")]
            public int Price { get; set; }
        }
    }
}
