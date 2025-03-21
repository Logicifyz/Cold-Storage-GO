﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class StaffArticleController : ControllerBase
    {
        private readonly DbContexts _context;

        public StaffArticleController(DbContexts context)
        {
            _context = context;
        }

        // Consolidated method to validate staff session and role
        private async Task<IActionResult> ValidateStaffSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is required.");
            }

            // Find the active staff session
            var staffSession = await _context.StaffSessions
                .FirstOrDefaultAsync(ss => ss.StaffSessionId == sessionId && ss.IsActive);

            if (staffSession == null)
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            // Ensure that the associated user has the 'staff' role
            var staff = await _context.Staff
                .FirstOrDefaultAsync(u => u.StaffId == staffSession.StaffId && u.Role == "staff");

            if (staff == null)
            {
                return Unauthorized("User is not a staff member.");
            }

            return Ok(staffSession.StaffId); // Return the StaffId for use
        }


        [HttpGet("articles")]
        public async Task<IActionResult> GetAllArticles(
    [FromQuery] string category = null,
    [FromQuery] string title = null,
    [FromQuery] Guid? staffId = null, // Filter for articles created/updated by a specific staff member
    [FromQuery] bool? highlighted = null, // Filter for highlighted articles
    [FromQuery] int? minViews = null, // Filter for minimum views
    [FromQuery] Guid? articleId = null // Added articleId filter
)
        {
            // Get the session ID from the cookies
            var sessionId = Request.Cookies["SessionId"];

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID is required.");
            }

            // Validate session
            var staffSession = await _context.StaffSessions
                .FirstOrDefaultAsync(ss => ss.StaffSessionId == sessionId && ss.IsActive);

            if (staffSession == null)
            {
                return Unauthorized("Invalid or inactive staff session.");
            }

            var query = _context.Articles.AsQueryable();

            // Apply filters if they are provided
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrEmpty(title))
            {
                query = query.Where(a => a.Title.Contains(title));
            }

            // Apply the staffId filter if provided
            if (staffId.HasValue)
            {
                query = query.Where(a => a.StaffId == staffId.Value);
            }

            // Apply highlighted filter if provided
            if (highlighted.HasValue)
            {
                query = query.Where(a => a.Highlighted == highlighted.Value);
            }

            // Apply views filter if minimum views is provided
            if (minViews.HasValue)
            {
                query = query.Where(a => a.Views >= minViews.Value);
            }

            // Apply the articleId filter if provided
            if (articleId.HasValue)
            {
                query = query.Where(a => a.ArticleId == articleId.Value);
            }

            var articles = await query.ToListAsync();
            return Ok(articles);
        }

        // Create a new article
        [HttpPost("articles")]
        public async Task<IActionResult> CreateArticle([FromBody] CreateArticleRequest request)
        {
            // Get the session ID from the request header
            var sessionId = Request.Cookies["SessionId"];

            // Validate the staff session and role
            var validationResponse = await ValidateStaffSession(sessionId);
            if (validationResponse is UnauthorizedResult)
            {
                return validationResponse;
            }

            // Extract the StaffId from the validation response
            var staffId = ((OkObjectResult)validationResponse).Value as Guid?;

            if (staffId == null)
            {
                return Unauthorized("Failed to identify staff member.");
            }

            // Create the new article
            var article = new Article
            {
                ArticleId = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                Category = request.Category,
                Highlighted = request.Highlighted,
                Views = 0, // Initialize with zero views
                StaffId = staffId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            // Save the article to the database
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return Ok(article);
        }

        [HttpPut("articles/{articleId}")]
        public async Task<IActionResult> UpdateArticle(Guid articleId, [FromBody] UpdateArticleRequest request)
        {
            // Get the session ID from the request header
            var sessionId = Request.Cookies["SessionId"];

            // Validate the staff session and role
            var validationResponse = await ValidateStaffSession(sessionId);
            if (validationResponse is UnauthorizedResult)
            {
                return validationResponse;
            }

            // Find the article by ID
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.ArticleId == articleId);
            if (article == null)
            {
                return NotFound("Article not found.");
            }

            // Update the article's fields
            if (!string.IsNullOrEmpty(request.Title))
            {
                article.Title = request.Title;
            }

            if (!string.IsNullOrEmpty(request.Content))
            {
                article.Content = request.Content;
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                article.Category = request.Category;
            }

            if (request.Highlighted.HasValue)
            {
                article.Highlighted = request.Highlighted.Value;
            }

            // Update the 'UpdatedAt' field
            article.UpdatedAt = DateTime.UtcNow;

            // Save changes to the database
            _context.Articles.Update(article);
            await _context.SaveChangesAsync();

            return Ok(article);
        }

        [HttpDelete("articles/{articleId}")]
        public async Task<IActionResult> DeleteArticle(Guid articleId)
        {
            // Get the session ID from the request header
            var sessionId = Request.Cookies["SessionId"];

            // Validate the staff session and role
            var validationResponse = await ValidateStaffSession(sessionId);
            if (validationResponse is UnauthorizedResult)
            {
                return validationResponse;
            }

            // Find the article by ID
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.ArticleId == articleId);
            if (article == null)
            {
                return NotFound("Article not found.");
            }

            // Delete the article
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Article deleted successfully." });
        }

    }


    // DTO for creating an article
    public class CreateArticleRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public bool Highlighted { get; set; } = false; // Default to not highlighted
    }

    public class UpdateArticleRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public bool? Highlighted { get; set; } // Nullable to allow no change
    }
}
