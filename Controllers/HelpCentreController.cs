using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelpCentreController : ControllerBase
    {
        private readonly DbContexts _context;

        public HelpCentreController(DbContexts context)
        {
            _context = context;
        }

        // Get articles with optional filters
        [HttpGet]
        public async Task<IActionResult> GetArticles(
            [FromQuery] string category = null,
            [FromQuery] bool? highlighted = null,
            [FromQuery] string search = null)
        {
            var query = _context.Articles.AsQueryable();

            // Filter by category if provided
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(article => article.Category == category);
            }

            // Filter by highlighted status if provided
            if (highlighted.HasValue)
            {
                query = query.Where(article => article.Highlighted == highlighted.Value);
            }

            // Filter by search keyword in title or content
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(article =>
                    article.Title.Contains(search) ||
                    article.Content.Contains(search));
            }

            // Order by created date descending (most recent first)
            query = query.OrderByDescending(article => article.CreatedAt);

            // Fetch articles from the database
            var articles = await query.ToListAsync();

            return Ok(articles);
        }

        [HttpPost("{articleId}/increment-views")]
        public async Task<IActionResult> IncrementViews(Guid articleId)
        {
            // Find the article by ID
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.ArticleId == articleId);
            if (article == null)
            {
                return NotFound("Article not found.");
            }

            // Increment the views
            article.Views += 1;

            // Save changes to the database
            _context.Articles.Update(article);
            await _context.SaveChangesAsync();
            return Ok(new { message = "View count incremented.", views = article.Views });
        }
    }
}
