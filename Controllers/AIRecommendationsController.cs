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
        public async Task<IActionResult> Recommend([FromBody] AdvancedControls controls)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Convert AdvancedControls to RecommendationRequest
            var request = controls.ToRecommendationRequest(Guid.NewGuid()); // Replace with actual UserId

            string responseMessage;

            // PRIORITIZE FreeText handling
            if (!string.IsNullOrWhiteSpace(request.FreeText))
            {
                Console.WriteLine($"DEBUG: FreeText detected. Using as prompt: {request.FreeText}");
                responseMessage = await CallOpenAIApi(request.FreeText);
            }
            else
            {
                // Fallback to structured prompt if FreeText is empty
                var structuredPrompt = BuildPromptFromRequest(request);
                Console.WriteLine($"DEBUG: Structured prompt generated: {structuredPrompt}");
                responseMessage = await CallOpenAIApi(structuredPrompt);
            }

            // Save the recommendation
            var aiRecommendation = new AIRecommendation
            {
                ChatId = Guid.NewGuid(),
                UserId = request.UserId,
                Message = responseMessage
            };

            _context.AIRecommendations.Add(aiRecommendation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Recommendation = responseMessage,
                ChatId = aiRecommendation.ChatId
            });
        }

        private string BuildPromptFromRequest(RecommendationRequest request)
        {
            var prompt = "Generate a recipe based on the following criteria:\n";

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

            return prompt;
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
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant for recipe generation." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500
                }), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"OpenAI API Error: {errorContent}");
                    return "Sorry, I couldn't generate a recipe due to an error.";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var reply = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                Console.WriteLine($"DEBUG: OpenAI Response: {reply}");
                return reply;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error communicating with OpenAI: {ex.Message}");
                return "An error occurred while generating the recipe.";
            }
        }
    }
}
