using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous] 
    [ApiController]
    [Route("api/[controller]")]
    public class DiscussionsController : ControllerBase
    {
        private readonly DbContexts _context;

        public DiscussionsController(DbContexts context)
        {
            _context = context;
        }

        // ✅ 1. Get All Discussions (With Filtering)
        [HttpGet]
        public async Task<IActionResult> GetDiscussions([FromQuery] string? category, [FromQuery] string? visibility)
        {
            var query = _context.Discussions
                .Include(d => d.CoverImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(d => d.Category == category);
            }

            if (!string.IsNullOrEmpty(visibility))
            {
                query = query.Where(d => d.Visibility == visibility);
            }

            var discussions = await query.ToListAsync();

            var formattedDiscussions = discussions.Select(discussion => new
            {
                discussion.DiscussionId,
                discussion.UserId,
                discussion.Title,
                discussion.Content,
                discussion.Category,
                discussion.Visibility,
                discussion.Upvotes,
                discussion.Downvotes,
                CoverImages = discussion.CoverImages?.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
            });

            return Ok(formattedDiscussions);
        }

        // ✅ 2. Get a Single Discussion by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscussion(string id)
        {
            Console.WriteLine($"🔍 [DEBUG] Incoming request for discussion ID: {id}");

            if (!Guid.TryParse(id, out Guid discussionGuid))
            {
                Console.WriteLine("❌ [ERROR] Invalid Discussion ID format.");
                return BadRequest("Invalid Discussion ID.");
            }

            var discussion = await _context.Discussions
                .Include(d => d.CoverImages)
                .FirstOrDefaultAsync(d => d.DiscussionId == discussionGuid);

            if (discussion == null)
            {
                Console.WriteLine("❌ [ERROR] Discussion not found in database.");
                return NotFound();
            }

            Console.WriteLine($"✅ [FOUND] Returning discussion: {discussion.Title}");

            var formattedDiscussion = new
            {
                discussion.DiscussionId,
                discussion.UserId,
                discussion.Title,
                discussion.Content,
                discussion.Category,
                discussion.Visibility,
                discussion.Upvotes,
                discussion.Downvotes,
                CoverImages = discussion.CoverImages?.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
            };

            return Ok(formattedDiscussion);
        }



        // ✅ 3. Create a Discussion (Matches `CreateRecipe`)
        [HttpPost]
        public async Task<IActionResult> CreateDiscussion(
            [FromForm] Discussion discussion,
            [FromForm] List<IFormFile>? coverImages)
        {
            // 🔹 Match session-based authentication in RecipesController
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("User is not logged in.");
            }

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
            {
                return Unauthorized("Invalid or expired session.");
            }

            discussion.DiscussionId = Guid.NewGuid();
            discussion.UserId = userSession.UserId;
            discussion.CoverImages = new List<DiscussionImage>();

            // ✅ Handle Image Uploads (Like Recipes)
            if (coverImages != null)
            {
                foreach (var image in coverImages)
                {
                    using (var ms = new MemoryStream())
                    {
                        await image.CopyToAsync(ms);
                        discussion.CoverImages.Add(new DiscussionImage
                        {
                            ImageId = Guid.NewGuid(),
                            ImageData = ms.ToArray(),
                            DiscussionId = discussion.DiscussionId
                        });
                    }
                }
            }

            _context.Discussions.Add(discussion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDiscussion), new { id = discussion.DiscussionId }, discussion);
        }
    }
}
