using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly DbContexts _context;

        public RecipesController(DbContexts context)
        {
            _context = context;
        }

        // 1. Get All Recipes (Search and Filter)
        [HttpGet]
        public async Task<IActionResult> GetRecipes([FromQuery] string? tags, [FromQuery] string? visibility)
        {
            var query = _context.Recipes.AsQueryable();

            if (!string.IsNullOrEmpty(tags))
            {
                query = query.Where(r => r.Tags.Contains(tags));
            }

            if (!string.IsNullOrEmpty(visibility))
            {
                query = query.Where(r => r.Visibility == visibility);
            }

            return Ok(await query.ToListAsync());
        }

        // 2. Get a Recipe by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipe(Guid id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return NotFound("Recipe not found.");

            return Ok(recipe);
        }

        // 3. Create a Recipe
        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromForm] Recipe recipe, [FromForm] List<IFormFile> mediaFiles)
        {
            recipe.RecipeId = Guid.NewGuid();

            // Ensure MediaFiles directory exists
            var mediaFolder = Path.Combine(Directory.GetCurrentDirectory(), "MediaFiles");
            if (!Directory.Exists(mediaFolder))
            {
                Directory.CreateDirectory(mediaFolder);
            }

            var mediaUrls = new List<string>();

            foreach (var file in mediaFiles)
            {
                if (file.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(mediaFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    mediaUrls.Add(Path.Combine("MediaFiles", fileName)); // Use relative path
                }
            }

            recipe.MediaUrls = mediaUrls;

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecipe), new { id = recipe.RecipeId }, recipe);
        }

        // 4. Update a Recipe
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(Guid id, [FromForm] Recipe updatedRecipe, [FromForm] List<IFormFile> mediaFiles)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return NotFound("Recipe not found.");

            recipe.Name = updatedRecipe.Name;
            recipe.Description = updatedRecipe.Description;
            recipe.TimeTaken = updatedRecipe.TimeTaken;
            recipe.Ingredients = updatedRecipe.Ingredients;
            recipe.Instructions = updatedRecipe.Instructions;
            recipe.Tags = updatedRecipe.Tags;
            recipe.Visibility = updatedRecipe.Visibility;

            // Ensure MediaFiles directory exists
            var mediaFolder = Path.Combine(Directory.GetCurrentDirectory(), "MediaFiles");
            if (!Directory.Exists(mediaFolder))
            {
                Directory.CreateDirectory(mediaFolder);
            }

            var mediaUrls = new List<string>();
            foreach (var file in mediaFiles)
            {
                if (file.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(mediaFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    mediaUrls.Add(Path.Combine("MediaFiles", fileName)); // Use relative path
                }
            }

            recipe.MediaUrls.AddRange(mediaUrls);

            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 5. Delete a Recipe
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(Guid id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return NotFound("Recipe not found.");

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
