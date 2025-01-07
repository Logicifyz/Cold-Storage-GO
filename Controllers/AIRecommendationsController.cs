using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIRecommendationsController : ControllerBase
    {
        private readonly DbContexts _context;

        public AIRecommendationsController(DbContexts context)
        {
            _context = context;
        }

        [HttpPost("Recommend")]
        public async Task<IActionResult> Recommend([FromBody] RecommendationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.UseChat || string.IsNullOrEmpty(request.Preference) || request.Ingredients == null || !request.Ingredients.Any())
            {
                var chatResponse = await StartChatWithAI(request.UserId);
                return Ok(new
                {
                    Message = "Chat-based interaction initiated",
                    ChatResponse = chatResponse
                });
            }

            var recommendation = GenerateStructuredRecommendation(request);

            var aiRecommendation = new AIRecommendation
            {
                ChatId = Guid.NewGuid(),
                UserId = request.UserId,
                Message = recommendation
            };

            _context.AIRecommendations.Add(aiRecommendation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecommendation), new { id = aiRecommendation.ChatId }, aiRecommendation);
        }

        private string GenerateStructuredRecommendation(RecommendationRequest request)
        {
            return $"Based on your ingredients: {string.Join(", ", request.Ingredients)} and preference: {request.Preference}, we recommend a customized recipe!";
        }

        private async Task<string> StartChatWithAI(Guid userId)
        {
            await Task.Delay(500); 
            return "Hello! Let me help you with a recipe. What ingredients do you have?";
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecommendation(Guid id)
        {
            var recommendation = await _context.AIRecommendations.FindAsync(id);
            if (recommendation == null) return NotFound("Recommendation not found.");

            return Ok(recommendation);
        }
    }
}
