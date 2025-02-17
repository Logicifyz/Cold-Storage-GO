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

            var sessionId = Request.Cookies["SessionId"];
            Guid? userId = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

                if (userSession != null)
                    userId = userSession.UserId;
            }

            var commentsQuery = _context.Comments
                .Include(c => c.User)
                .ThenInclude(u => u.UserProfile) // Include User Profile for Profile Picture
                .Where(c => c.IsDeleted == false)
                .AsQueryable();

            if (recipeId != null)
                commentsQuery = commentsQuery.Where(c => c.RecipeId == recipeId);
            else
                commentsQuery = commentsQuery.Where(c => c.DiscussionId == discussionId);

            var comments = await commentsQuery.ToListAsync();

            // Get all votes for these comments
            var commentIds = comments.Select(c => c.CommentId).ToList();
            var votes = await _context.CommentVotes
                .Where(v => commentIds.Contains(v.CommentId))
                .ToListAsync();

            var voteLookup = comments.ToDictionary(
                c => c.CommentId,
                c => votes.Where(v => v.CommentId == c.CommentId).Any()
                    ? new
                    {
                        Upvotes = votes.Count(v => v.CommentId == c.CommentId && v.VoteType == 1),
                        Downvotes = votes.Count(v => v.CommentId == c.CommentId && v.VoteType == -1),
                        UserVote = userId.HasValue
                            ? votes.FirstOrDefault(v => v.CommentId == c.CommentId && v.UserId == userId)?.VoteType ?? 0
                            : 0
                    }
                    : new { Upvotes = 0, Downvotes = 0, UserVote = 0 }
            );

            // ✅ Restore Nested Structure
            var commentDict = new Dictionary<Guid, dynamic>();

            var formattedComments = comments.Select(comment => new
            {
                comment.CommentId,
                comment.Content,
                comment.CreatedAt,
                comment.ParentCommentId,
                Upvotes = voteLookup[comment.CommentId].Upvotes,
                Downvotes = voteLookup[comment.CommentId].Downvotes,
                UserVote = voteLookup[comment.CommentId].UserVote,
                Username = comment.User?.Username ?? "Unknown",
                ProfilePicture = comment.User?.UserProfile?.ProfilePicture != null
                    ? Convert.ToBase64String(comment.User.UserProfile.ProfilePicture)
                    : null,
                Replies = new List<object>() // This will be populated later
            }).ToDictionary(c => c.CommentId);

            // ✅ Assign Replies to Their Parents
            List<object> rootComments = new List<object>();

            foreach (var comment in formattedComments.Values)
            {
                if (comment.ParentCommentId.HasValue && formattedComments.ContainsKey(comment.ParentCommentId.Value))
                {
                    var parentComment = formattedComments[comment.ParentCommentId.Value];
                    ((List<object>)parentComment.Replies).Add(comment);
                }
                else
                {
                    rootComments.Add(comment);
                }
            }

            // ✅ Sort First by Upvotes, Then by CreatedAt (Newer at Bottom)
            void SortComments(List<object> commentsList)
            {
                commentsList.Sort((a, b) =>
                {
                    dynamic commentA = a, commentB = b;
                    int voteCompare = commentB.Upvotes.CompareTo(commentA.Upvotes);
                    if (voteCompare != 0) return voteCompare; // Sort by upvotes first
                    return commentA.CreatedAt.CompareTo(commentB.CreatedAt); // Sort by createdAt if upvotes are equal
                });

                foreach (var comment in commentsList)
                {
                    dynamic dynamicComment = comment;
                    if (dynamicComment.Replies.Count > 0)
                    {
                        SortComments(dynamicComment.Replies);
                    }
                }
            }

            SortComments(rootComments);

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


        [HttpGet("user/{username}")]
        public async Task<IActionResult> GetUserComments(string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest("Username is required.");

            var sessionId = Request.Cookies["SessionId"];
            Guid? userId = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
                if (userSession != null)
                    userId = userSession.UserId;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("User not found.");

            var comments = await _context.Comments
                .Where(c => c.UserId == user.UserId)
                .Select(comment => new
                {
                    comment.CommentId,
                    comment.Content,
                    comment.CreatedAt,
                    comment.RecipeId,
                    comment.DiscussionId
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpGet("my-comments")]
        public async Task<IActionResult> GetMyComments()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session not found.");

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
                return Unauthorized("Invalid session.");

            var comments = await _context.Comments
                .Where(c => c.UserId == userSession.UserId)
                .Select(comment => new
                {
                    comment.CommentId,
                    comment.Content,
                    comment.CreatedAt,
                    comment.RecipeId,
                    comment.DiscussionId,
                    comment.ParentCommentId,

                    // Fetch Parent Comment details (if exists)
                    ParentComment = comment.ParentCommentId != null
                        ? _context.Comments
                            .Where(p => p.CommentId == comment.ParentCommentId)
                            .Select(p => new { p.CommentId, p.Content, Username = p.User.Username })
                            .FirstOrDefault()
                        : null,

                    // Fetch Discussion Title if comment is on a discussion
                    DiscussionTitle = comment.DiscussionId != null
                        ? _context.Discussions
                            .Where(d => d.DiscussionId == comment.DiscussionId)
                            .Select(d => d.Title)
                            .FirstOrDefault()
                        : null,

                    // Fetch Recipe Title if comment is on a recipe
                    RecipeTitle = comment.RecipeId != null
                        ? _context.Recipes
                            .Where(r => r.RecipeId == comment.RecipeId)
                            .Select(r => r.Name)
                            .FirstOrDefault()
                        : null,

                    comment.User.Username,  // Username of the person who posted the comment

                    // Upvotes & Downvotes count
                    Upvotes = _context.CommentVotes.Count(v => v.CommentId == comment.CommentId && v.VoteType == 1),
                    Downvotes = _context.CommentVotes.Count(v => v.CommentId == comment.CommentId && v.VoteType == -1),
                })
                .OrderByDescending(c => c.CreatedAt) // Sort newest first
                .ToListAsync();

            return Ok(comments);
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
                    // ✅ If user clicks again on the same vote, remove the vote
                    _context.CommentVotes.Remove(existingVote);
                    if (voteType == 1) comment.Upvotes--;
                    else comment.Downvotes--;
                }
                else
                {
                    // ✅ If user changes their vote (upvote -> downvote or vice versa)
                    if (existingVote.VoteType == 1)
                    {
                        comment.Upvotes--;
                        comment.Downvotes++;
                    }
                    else
                    {
                        comment.Downvotes--;
                        comment.Upvotes++;
                    }

                    existingVote.VoteType = voteType;
                }
            }
            else
            {
                // ✅ New Vote
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

            // ✅ Send updated vote counts to frontend immediately
            return Ok(new { upvotes = comment.Upvotes, downvotes = comment.Downvotes });
        }


    }
}
