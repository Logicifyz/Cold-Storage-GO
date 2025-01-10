using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

        // Get All 
        [HttpGet]
        public async Task<IActionResult> GetDiscussions([FromQuery] string? category, [FromQuery] string? visibility)
        {
            var query = _context.Discussions.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(d => d.Category == category);
            }

            if (!string.IsNullOrEmpty(visibility))
            {
                query = query.Where(d => d.Visibility == visibility);
            }

            return Ok(await query.ToListAsync());
        }

        // Get by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscussion(Guid id)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound("Discussion not found.");

            return Ok(discussion);
        }

        // Post
        [HttpPost]
        public async Task<IActionResult> CreateDiscussion([FromBody] Discussion discussion)
        {
            discussion.DiscussionId = Guid.NewGuid();
            _context.Discussions.Add(discussion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDiscussion), new { id = discussion.DiscussionId }, discussion);
        }

        // Put
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscussion(Guid id, [FromBody] Discussion updatedDiscussion)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound("Discussion not found.");

            discussion.Title = updatedDiscussion.Title;
            discussion.Content = updatedDiscussion.Content;
            discussion.Category = updatedDiscussion.Category;
            discussion.Visibility = updatedDiscussion.Visibility;
            discussion.Upvotes = updatedDiscussion.Upvotes;
            discussion.Downvotes = updatedDiscussion.Downvotes;

            _context.Discussions.Update(discussion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscussion(Guid id)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound("Discussion not found.");

            _context.Discussions.Remove(discussion);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
