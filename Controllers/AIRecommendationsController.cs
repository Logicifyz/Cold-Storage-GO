using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AIRecommendationsController : ControllerBase
    {
        private readonly DbContexts _context;
        private readonly string _apiKey;

        public AIRecommendationsController(DbContexts context, IConfiguration configuration)
        {
            _context = context;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        [HttpPost("Recommend")]
        public async Task<IActionResult> Recommend([FromBody] UserRecipeRequest userRequest)
        {
            Console.WriteLine($"DEBUG: Received User Request - {JsonSerializer.Serialize(userRequest)}");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (userRequest.UserId == Guid.Empty)
            {
                Console.WriteLine("WARNING: UserId not provided. Fetching default user...");
                var defaultUser = await _context.Users.FirstOrDefaultAsync();
                if (defaultUser == null)
                {
                    Console.WriteLine("ERROR: No users exist in the database.");
                    return BadRequest("No users found in the system.");
                }
                userRequest.UserId = defaultUser.UserId;
            }

            var user = await _context.Users.FindAsync(userRequest.UserId);
            if (user == null)
            {
                return BadRequest("Invalid UserId. User does not exist.");
            }

            // ✅ Convert UserRecipeRequest to AIRecipeRequest
            var aiRequest = userRequest.ToAIRecipeRequest(userRequest.UserId);

            // ✅ Save the AI request to the database
            _context.AIRecipeRequests.Add(aiRequest);
            await _context.SaveChangesAsync();

            Console.WriteLine($"DEBUG: AIRecipeRequest saved to DB - {JsonSerializer.Serialize(aiRequest)}");

            // ✅ Call AI with structured AIRecipeRequest
            string responseMessage = await CallOpenAIApi(BuildPromptFromRequest(aiRequest));
            Console.WriteLine($"DEBUG: AI Raw Response: {responseMessage}");

            if (string.IsNullOrWhiteSpace(responseMessage) || responseMessage.Trim() == "{}")
            {
                return BadRequest("Invalid AI response.");
            }

            // ✅ Ensure AI response is properly formatted JSON
            JsonElement jsonResponse;
            try
            {
                jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseMessage);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"ERROR: Failed to parse AI response JSON - {ex.Message}");
                return BadRequest("AI response is not valid JSON.");
            }

            // ✅ Extract responseType from AI response correctly
            if (!jsonResponse.TryGetProperty("responseType", out var responseTypeElement) ||
    string.IsNullOrWhiteSpace(responseTypeElement.GetString()))
            {
                Console.WriteLine("WARNING: responseType missing from AI response. Defaulting to FollowUp.");
                responseTypeElement = JsonDocument.Parse("{\"responseType\": \"FollowUp\"}").RootElement.GetProperty("responseType");
            }

            string responseTypeString = responseTypeElement.GetString() ?? "FollowUp";

            if (!Enum.TryParse(responseTypeString, true, out ResponseType responseType))
            {
                Console.WriteLine($"WARNING: Invalid responseType '{responseTypeString}', defaulting to Unknown.");
                responseType = ResponseType.Unknown;
            }

            Console.WriteLine($"DEBUG: Parsed responseType - {responseType}");

            // ✅ Save AI Response Log
            var aiResponse = new AIResponseLog
            {
                UserId = userRequest.UserId,
                Message = responseMessage,
                Type = responseType,
                Timestamp = DateTime.UtcNow
            };

            _context.AIResponseLogs.Add(aiResponse);
            await _context.SaveChangesAsync();

            // ✅ Handle different response types
            if (responseType == ResponseType.Recipe)
            {
                var finalDish = ParseFinalDish(responseMessage);
                if (finalDish != null)
                {
                    finalDish.UserId = userRequest.UserId;
                    _context.FinalDishes.Add(finalDish);
                    await _context.SaveChangesAsync();
                }

                var latestFinalDish = await _context.FinalDishes
                    .Where(d => d.UserId == userRequest.UserId)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    responseType = ResponseType.Recipe.ToString(),
                    recipe = latestFinalDish
                });
            }
            else if (responseType == ResponseType.Redirect || responseType == ResponseType.FollowUp)
            {
                Console.WriteLine($"DEBUG: Handling responseType - {responseType}");

                if (jsonResponse.TryGetProperty("message", out var messageElement))
                {
                    string followUpMessage = messageElement.GetString();
                    Console.WriteLine($"DEBUG: AI FollowUp Message: {followUpMessage}");
                    return Ok(new
                    {
                        responseType = responseType.ToString(),  // Ensure responseType is included
                        message = followUpMessage
                    });
                }

                return Ok(new
                {
                    responseType = responseType.ToString(),
                    message = "Can you clarify your request?"
                });
            }
            else
            {
                return BadRequest("Unexpected response from AI.");
            }
        }



        private string BuildPromptFromRequest(AIRecipeRequest request)
        {
            var prompt = "Generate a detailed cooking recipe based on the following criteria:\n";

            if (!string.IsNullOrWhiteSpace(request.FreeText))
                prompt += $"User Request: {request.FreeText}\n"; // ✅ Ensure user's request is always included

            if (request.Ingredients.Any())
                prompt += $"Ingredients: {string.Join(", ", request.Ingredients)}\n";
            if (!string.IsNullOrEmpty(request.Preference))
                prompt += $"Preference: {request.Preference}\n";
            if (request.DietaryPreferences.Any())
                prompt += $"Dietary Preferences: {string.Join(", ", request.DietaryPreferences)}\n";
            if (request.ExcludeIngredients.Any())
                prompt += $"Exclude: {string.Join(", ", request.ExcludeIngredients)}\n";
            if (request.MaxIngredients.HasValue)
                prompt += $"Limit to {request.MaxIngredients} ingredients.\n";
            if (!string.IsNullOrEmpty(request.CookingTime))
                prompt += $"Ensure cooking time is under {request.CookingTime}.\n";
            if (request.Servings.HasValue)
                prompt += $"The recipe should serve {request.Servings.Value} people.\n"; // ✅ Ensure servings are included

            return prompt;
        }

        private async Task<string> FetchTrendingFood()
        {
            // Simulate fetching a trending food idea (this should ideally fetch from an API)
            return "One of the trending dishes right now is 'Butter Candle Bread'! Want a recipe?";
        }

        private async Task<string> CallOpenAIApi(string prompt)
        {
            try
            {
                Console.WriteLine($"DEBUG: Sending Prompt to OpenAI: {prompt}");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = "gpt-4",
                    messages = new[]
                    {
                        new { role = "system", content = @"
                            You are an AI assistant that ONLY outputs structured JSON.
                            ALWAYS return the response in the EXACT format below:

                            {
                                'responseType': 'Recipe' | 'FollowUp' | 'Redirect',
                                'title': 'Recipe Name',
                                'description': 'Brief description of the recipe',
                                'ingredients': ['Ingredient 1', 'Ingredient 2'],
                                'steps': ['Step 1', 'Step 2'],
                                'nutrition': { 'calories': 350, 'protein': 25, 'carbs': 40, 'fats': 15 },
                                'servings': 1,
                                'cookingTime': '30 minutes',
                                'difficulty': 'Medium',
                                'createdAt': 'YYYY-MM-DDTHH:MM:SSZ'
                            }

                             STRICT RULES:
                            - **responseType is MANDATORY**. If missing, return `""responseType"": ""FollowUp""`.
                            - If the request is unclear, use `""responseType"": ""FollowUp""`.
                            - If the request is **not food-related**, use `""responseType"": ""Redirect""`.
                            - `calories`, `protein`, `carbs`, and `fats` **must be integers**.
                            - NEVER include additional explanations or formatting.
                            - Ensure `calories`, `protein`, `carbs`, and `fats` are **always integers**, without units like 'g' or 'kcal'.
                        " },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500
                }), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"OpenAI API Error: {errorContent}");
                    return "{}";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG: Raw OpenAI Response: {responseContent}");

                // Extract the actual message content from OpenAI
                JsonElement result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                string aiMessage = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                Console.WriteLine($"DEBUG: Extracted AI Message: {aiMessage}");

                return aiMessage;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to call OpenAI API: {ex.Message}");
                return "{}";
            }
        }


        private FinalDish? ParseFinalDish(string responseMessage)
        {
            try
            {
                Console.WriteLine($"DEBUG: Attempting to parse AI response: {responseMessage}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                if (string.IsNullOrWhiteSpace(responseMessage) || !responseMessage.TrimStart().StartsWith("{"))
                {
                    Console.WriteLine("ERROR: AI Response is not a valid JSON object.");
                    return null;
                }

                var parsedDish = JsonSerializer.Deserialize<FinalDish>(responseMessage, options);

                if (parsedDish == null || string.IsNullOrWhiteSpace(parsedDish.Title))
                {
                    Console.WriteLine("DEBUG: AI returned an invalid recipe format.");
                    return null;
                }

                // ✅ Ensure missing fields have default values
                parsedDish.CreatedAt = DateTime.UtcNow;
                parsedDish.Difficulty = string.IsNullOrWhiteSpace(parsedDish.Difficulty) ? "Medium" : parsedDish.Difficulty;
                parsedDish.Servings = parsedDish.Servings > 0 ? parsedDish.Servings : 1;
                parsedDish.CookingTime = string.IsNullOrWhiteSpace(parsedDish.CookingTime) ? "30 minutes" : parsedDish.CookingTime;

                // ✅ Ensure Ingredients, Steps, and Tags are stored as **lists** (not JSON strings)
                parsedDish.Ingredients = parsedDish.Ingredients?.Select(i => i.Trim()).ToList() ?? new List<string>();
                parsedDish.Steps = parsedDish.Steps?.Select(s => s.Trim()).ToList() ?? new List<string>();
                parsedDish.Tags = parsedDish.Tags?.Select(t => t.Trim()).ToList() ?? new List<string>();

                // ✅ Ensure Nutrition is properly initialized
                parsedDish.Nutrition ??= new NutritionInfo
                {
                    Calories = parsedDish.Nutrition?.Calories ?? 0,
                    Protein = parsedDish.Nutrition?.Protein ?? 0,
                    Carbs = parsedDish.Nutrition?.Carbs ?? 0,
                    Fats = parsedDish.Nutrition?.Fats ?? 0
                };

                Console.WriteLine("DEBUG: Successfully parsed FinalDish object.");
                return parsedDish;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to parse AI response into FinalDish: {ex.Message}");
                return null;
            }
        }

        [HttpGet("GetLatestRecipe")]
        public async Task<IActionResult> GetLatestRecipe()
        {
            var latestFinalDish = await _context.FinalDishes
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestFinalDish == null)
            {
                return NotFound("No recipes found.");
            }

            return Ok(latestFinalDish);
        }


    }
}
