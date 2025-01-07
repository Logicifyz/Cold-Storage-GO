using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly DbContexts _context;

        public CommentsController(DbContexts context)
        {
            _context = context;
        }

        // Get all 
        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] Guid? recipeId, [FromQuery] Guid? discussionId)
        {
            if (recipeId == null && discussionId == null)
            {
                return BadRequest("Either recipeId or discussionId must be provided.");
            }

            var query = _context.Comments.AsQueryable();

            if (recipeId != null)
            {
                query = query.Where(c => c.RecipeId == recipeId);
            }
            else if (discussionId != null)
            {
                query = query.Where(c => c.DiscussionId == discussionId);
            }

            return Ok(await query.ToListAsync());
        }

        // Get by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(Guid id)
        {
            var comment = await _context.Comments
                .Include(c => c.Replies) // Include replies for threading
                .FirstOrDefaultAsync(c => c.CommentId == id);

            if (comment == null) return NotFound("Comment not found.");

            return Ok(comment);
        }

        // Post
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] Comment comment)
        {
            if (comment.ParentCommentId != null)
            {
                // Ensure the parent comment exists
                var parentComment = await _context.Comments.FindAsync(comment.ParentCommentId);
                if (parentComment == null)
                {
                    return BadRequest("Parent comment does not exist.");
                }
            }

            comment.CommentId = Guid.NewGuid();
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetComment), new { id = comment.CommentId }, comment);
        }

        // Put
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(Guid id, [FromBody] Comment updatedComment)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound("Comment not found.");

            comment.Content = updatedComment.Content;
            comment.Upvotes = updatedComment.Upvotes;
            comment.Downvotes = updatedComment.Downvotes;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound("Comment not found.");

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
