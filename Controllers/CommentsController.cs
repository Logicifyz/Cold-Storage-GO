using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly DbContexts _context;

        public CommentsController(DbContexts context)
        {
            _context = context;
        }

        // ✅ GET all comments for a specific Recipe or Discussion
        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] Guid? recipeId, [FromQuery] Guid? discussionId)
        {
            if (recipeId == null && discussionId == null)
                return BadRequest("Either recipeId or discussionId must be provided.");

            var commentsQuery = _context.Comments
            .Include(c => c.User)
            .ThenInclude(u => u.UserProfile) // ✅ Include User Profile for Profile Picture
            .Where(c => c.IsDeleted == false)
            .OrderBy(c => c.CreatedAt)
            .AsQueryable();

            // ✅ Only return comments associated with the correct Recipe/Discussion
            if (recipeId != null)
                commentsQuery = commentsQuery.Where(c => c.RecipeId == recipeId);
            else
                commentsQuery = commentsQuery.Where(c => c.DiscussionId == discussionId);

            var comments = await commentsQuery.ToListAsync();



            // Convert list to dictionary for fast lookup
            var commentDict = comments.ToDictionary(c => c.CommentId);

            // Ensure each comment has a replies list
            foreach (var comment in comments)
            {
                comment.Replies = new List<Comment>(); // Prevent duplication
            }

            // Organize comments into a parent-child hierarchy
            List<object> rootComments = new List<object>();
            Dictionary<Guid, object> formattedComments = new Dictionary<Guid, object>();

            foreach (var comment in comments)
            {
                var formattedComment = new
                {
                    comment.CommentId,
                    comment.Content,
                    comment.CreatedAt,
                    comment.ParentCommentId,
                    comment.Upvotes,
                    comment.Downvotes,
                    Username = comment.User?.Username ?? "Unknown",
                    ProfilePicture = comment.User?.UserProfile?.ProfilePicture != null ?
                        Convert.ToBase64String(comment.User.UserProfile.ProfilePicture) : null,
                    Replies = new List<object>() // Will populate later
                };

                formattedComments[comment.CommentId] = formattedComment;
            }

            // Assign replies to their parents
            foreach (var comment in comments)
            {
                if (comment.ParentCommentId.HasValue && formattedComments.ContainsKey(comment.ParentCommentId.Value))
                {
                    var parentComment = (dynamic)formattedComments[comment.ParentCommentId.Value];
                    ((List<object>)parentComment.Replies).Add(formattedComments[comment.CommentId]);
                }
                else
                {
                    rootComments.Add(formattedComments[comment.CommentId]);
                }
            }

            return Ok(rootComments);
        }



        // ✅ GET a single comment by ID (including replies)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(Guid id)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.CommentId == id && c.IsDeleted == false);

            if (comment == null)
                return NotFound("Comment not found.");

            return Ok(comment);
        }

        // ✅ POST a new comment (Requires Authenticated User)
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] Comment comment)
        {
            Console.WriteLine($"[DEBUG] Received Comment Request: {System.Text.Json.JsonSerializer.Serialize(comment)}");

            if (comment.UserId == Guid.Empty)
            {
                return BadRequest(new { message = "UserId is required." });
            }

            if (comment.RecipeId == null && comment.DiscussionId == null)
            {
                return BadRequest(new { message = "Either RecipeId or DiscussionId must be provided." });
            }

            // Ensure the correct post type is set
            if (comment.RecipeId != null && comment.DiscussionId != null)
            {
                return BadRequest(new { message = "A comment cannot belong to both a recipe and a discussion." });
            }

            if (comment.ParentCommentId != null)
            {
                var parentCommentExists = await _context.Comments.AnyAsync(c => c.CommentId == comment.ParentCommentId);
                if (!parentCommentExists)
                    return BadRequest("Parent comment does not exist.");
            }

            // Assign defaults
            comment.CommentId = Guid.NewGuid();
            comment.CreatedAt = DateTime.UtcNow;
            comment.Upvotes = 0;
            comment.Downvotes = 0;
            comment.IsDeleted = false;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] Successfully Created Comment ID: {comment.CommentId}");

            return CreatedAtAction(nameof(GetComment), new { id = comment.CommentId }, comment);
        }




        // ✅ PUT (Update) a Comment (Only by Owner)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(Guid id, [FromBody] Comment updatedComment)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized(new { message = "User is not logged in." });

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
                return Unauthorized(new { message = "Invalid or expired session." });

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null || comment.IsDeleted)
                return NotFound("Comment not found.");

            if (comment.UserId != userSession.UserId)
                return Forbid("You can only edit your own comments.");

            comment.Content = updatedComment.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ DELETE (Soft Delete) a Comment (Only by Owner)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized(new { message = "User is not logged in." });

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
                return Unauthorized(new { message = "Invalid or expired session." });

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null || comment.IsDeleted)
                return NotFound("Comment not found.");

            if (comment.UserId != userSession.UserId)
                return Forbid("You can only delete your own comments.");

            // Soft delete: Mark as deleted instead of removing from DB
            comment.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ POST: Upvote or Downvote a Comment
        [HttpPost("{id}/vote")]
        public async Task<IActionResult> VoteComment(Guid id, [FromBody] int voteType)
        {
            if (voteType != -1 && voteType != 1)
                return BadRequest("Invalid vote type. Use -1 for downvote and 1 for upvote.");

            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized(new { message = "User is not logged in." });

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
                return Unauthorized(new { message = "Invalid or expired session." });

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null || comment.IsDeleted)
                return NotFound("Comment not found.");

            var existingVote = await _context.CommentVotes
                .FirstOrDefaultAsync(cv => cv.CommentId == id && cv.UserId == userSession.UserId);

            if (existingVote != null)
            {
                if (existingVote.VoteType == voteType)
                {
                    _context.CommentVotes.Remove(existingVote);
                    if (voteType == 1) comment.Upvotes--;
                    else comment.Downvotes--;
                }
                else
                {
                    existingVote.VoteType = voteType;
                    if (voteType == 1) { comment.Upvotes++; comment.Downvotes--; }
                    else { comment.Downvotes++; comment.Upvotes--; }
                }
            }
            else
            {
                var newVote = new CommentVote
                {
                    VoteId = Guid.NewGuid(),
                    CommentId = id,
                    UserId = userSession.UserId,
                    VoteType = voteType
                };
                _context.CommentVotes.Add(newVote);
                if (voteType == 1) comment.Upvotes++;
                else comment.Downvotes++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { upvotes = comment.Upvotes, downvotes = comment.Downvotes });
        }
    }
}
