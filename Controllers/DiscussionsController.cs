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
            var sessionId = Request.Cookies["SessionId"];
            Guid? userId = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
                if (userSession != null)
                    userId = userSession.UserId;
            }

            var query = from discussion in _context.Discussions
                        join user in _context.Users on discussion.UserId equals user.UserId
                        join userProfile in _context.UserProfiles on user.UserId equals userProfile.UserId
                        select new
                        {
                            discussion.DiscussionId,
                            discussion.UserId,
                            User = new
                            {
                                user.Username,
                                userProfile.ProfilePicture
                            },
                            discussion.Title,
                            discussion.Content,
                            discussion.Category,
                            discussion.Visibility,
                            discussion.Upvotes,
                            discussion.Downvotes,
                            CoverImages = discussion.CoverImages != null
                                ? discussion.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                                : new List<string>(),
                            Votes = discussion.Votes
                        };

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
                User = discussion.User,
                discussion.Title,
                discussion.Content,
                discussion.Category,
                discussion.Visibility,
                discussion.Upvotes,
                discussion.Downvotes,
                userVote = userId.HasValue
                    ? discussion.Votes.FirstOrDefault(v => v.UserId == userId)?.VoteType ?? 0
                    : 0,
                discussion.CoverImages
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
                .Include(d => d.Votes) // Include votes
                .FirstOrDefaultAsync(d => d.DiscussionId == discussionGuid);

            if (discussion == null)
            {
                Console.WriteLine("❌ [ERROR] Discussion not found in database.");
                return NotFound();
            }

            Console.WriteLine($"✅ [FOUND] Returning discussion: {discussion.Title}");

            var sessionId = Request.Cookies["SessionId"];
            Guid? userId = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
                if (userSession != null)
                    userId = userSession.UserId;
            }

            var userVote = userId.HasValue
                ? discussion.Votes.FirstOrDefault(v => v.UserId == userId)?.VoteType ?? 0
                : 0;

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
                userVote, // Include user's vote state
                CoverImages = discussion.CoverImages?.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
            };

            return Ok(formattedDiscussion);
        }

        [HttpGet("user/{username}")]
        public async Task<IActionResult> GetUserDiscussions(string username)
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

            var discussions = await _context.Discussions
                .Where(d => d.UserId == user.UserId)
                .Select(discussion => new
                {
                    discussion.DiscussionId,
                    discussion.Title,
                    discussion.Content,
                    discussion.Upvotes,
                    discussion.Downvotes,
                    CoverImages = discussion.CoverImages != null
                        ? discussion.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                        : new List<string>()
                })
                .ToListAsync();

            return Ok(discussions);
        }

        [HttpGet("my-discussions")]
        public async Task<IActionResult> GetMyDiscussions()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session not found.");

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
                return Unauthorized("Invalid session.");

            var discussions = await _context.Discussions
                .Where(d => d.UserId == userSession.UserId)
                .Select(discussion => new
                {
                    discussion.DiscussionId,
                    discussion.Title,
                    discussion.Content,
                    discussion.Visibility,  // ✅ Includes visibility for private/public logic
                    discussion.Upvotes,
                    discussion.Downvotes,
                    CoverImages = discussion.CoverImages != null
                        ? discussion.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                        : new List<string>()
                })
                .ToListAsync();

            return Ok(discussions);
        }



        [HttpPost("{id}/vote")]
        public async Task<IActionResult> VoteDiscussion(Guid id, [FromBody] int voteType)
        {
            if (voteType != -1 && voteType != 1)
                return BadRequest("Invalid vote type. Use -1 for downvote and 1 for upvote.");

            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized(new { message = "User must be logged in." });

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
                return Unauthorized(new { message = "Invalid or expired session." });

            var userId = userSession.UserId;

            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null)
                return NotFound("Discussion not found.");

            var existingVote = await _context.DiscussionVotes
                .FirstOrDefaultAsync(v => v.DiscussionId == id && v.UserId == userId);

            if (existingVote != null)
            {
                if (existingVote.VoteType == voteType)
                {
                    // ✅ Remove vote if clicked again
                    _context.DiscussionVotes.Remove(existingVote);

                    // ✅ Adjust the vote counts
                    if (voteType == 1)
                        discussion.Upvotes = Math.Max(0, discussion.Upvotes - 1); // Decrease upvotes
                    else if (voteType == -1)
                        discussion.Downvotes = Math.Max(0, discussion.Downvotes - 1); // Decrease downvotes
                }
                else
                {
                    // ✅ Change vote (Upvote <-> Downvote)
                    if (existingVote.VoteType == 1)
                    {
                        discussion.Upvotes = Math.Max(0, discussion.Upvotes - 1); // Decrease upvotes
                        discussion.Downvotes += 1; // Increase downvotes
                    }
                    else if (existingVote.VoteType == -1)
                    {
                        discussion.Downvotes = Math.Max(0, discussion.Downvotes - 1); // Decrease downvotes
                        discussion.Upvotes += 1; // Increase upvotes
                    }

                    existingVote.VoteType = voteType; // Update the vote type
                    _context.DiscussionVotes.Update(existingVote);
                }
            }
            else
            {
                // ✅ Add new vote
                var newVote = new DiscussionVote
                {
                    VoteId = Guid.NewGuid(),
                    DiscussionId = id,
                    UserId = userId,
                    VoteType = voteType
                };
                _context.DiscussionVotes.Add(newVote);

                // ✅ Adjust the vote counts
                if (voteType == 1)
                    discussion.Upvotes += 1; // Increase upvotes
                else if (voteType == -1)
                    discussion.Downvotes += 1; // Increase downvotes
            }

            await _context.SaveChangesAsync();

            var userVote = await _context.DiscussionVotes
               .Where(v => v.DiscussionId == id && v.UserId == userId)
               .Select(v => v.VoteType)
               .FirstOrDefaultAsync();

            return Ok(new
            {
                upvotes = discussion.Upvotes,
                downvotes = discussion.Downvotes,
                voteScore = discussion.Upvotes - discussion.Downvotes,
                userVote = userVote
            });
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


        [HttpPut("{discussionId}")]
        public async Task<IActionResult> UpdateDiscussion(Guid discussionId, [FromForm] Discussion updatedDiscussion, [FromForm] List<IFormFile>? coverImages)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId)) return Unauthorized("Session not found.");

            var userSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null) return Unauthorized("Invalid session.");

            var discussion = await _context.Discussions.Include(d => d.CoverImages).FirstOrDefaultAsync(d => d.DiscussionId == discussionId);
            if (discussion == null) return NotFound("Discussion not found.");

            // Ensure only the owner can edit
            if (discussion.UserId != userSession.UserId) return Forbid();

            // Update discussion properties
            discussion.Title = updatedDiscussion.Title;
            discussion.Content = updatedDiscussion.Content;
            discussion.Category = updatedDiscussion.Category;
            discussion.Visibility = updatedDiscussion.Visibility;

            // Handle image updates (replace old images if new ones are provided)
            if (coverImages != null && coverImages.Count > 0)
            {
                // Delete existing images
                _context.DiscussionImages.RemoveRange(discussion.CoverImages);
                discussion.CoverImages = new List<DiscussionImage>();

                foreach (var file in coverImages)
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    discussion.CoverImages.Add(new DiscussionImage { ImageData = ms.ToArray() });
                }
            }

            _context.Discussions.Update(discussion);
            await _context.SaveChangesAsync();

            return Ok("Discussion updated successfully.");
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscussion(Guid id)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId)) return Unauthorized("Session not found.");

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null) return Unauthorized("Invalid session.");

            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound("Discussion not found.");

            // Ensure user owns the discussion
            if (discussion.UserId != userSession.UserId)
            {
                return Forbid("You do not have permission to delete this discussion.");
            }

            _context.Discussions.Remove(discussion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Discussion deleted successfully" });
        }


    }
}
