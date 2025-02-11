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
        public async Task<IActionResult> GetRecipes()
        {
            var recipes = await _context.Recipes
                .Include(r => r.CoverImages)
                .ToListAsync();

            Console.WriteLine($"Returning {recipes.Count} recipes...");

            var formattedRecipes = recipes.Select(recipe => new
            {
                RecipeId = recipe.RecipeId,
                UserId = recipe.UserId,
                DishId = recipe.DishId,
                Name = recipe.Name,
                Description = recipe.Description,
                TimeTaken = recipe.TimeTaken,
                Tags = recipe.Tags,
                Visibility = recipe.Visibility,
                Upvotes = recipe.Upvotes,
                Downvotes = recipe.Downvotes,
                CoverImages = recipe.CoverImages != null && recipe.CoverImages.Any()
                    ? recipe.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                    : new List<string>(), // Ensure an empty array if no images exist
                Ingredients = recipe.Ingredients,
                Instructions = recipe.Instructions
            }).ToList();

            return Ok(formattedRecipes);
        }





        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipe(Guid id)
        {
            Console.WriteLine($"Fetching recipe with ID: {id}");
            if (id == Guid.Empty)
            {
                return BadRequest("Recipe ID is missing.");
            }

            var recipe = await _context.Recipes
                .Include(r => r.CoverImages)
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
            {
                return NotFound("Recipe not found.");
            }

            var formattedRecipe = new
            {
                RecipeId = recipe.RecipeId,
                Name = recipe.Name,
                Description = recipe.Description,
                TimeTaken = recipe.TimeTaken,
                Tags = !string.IsNullOrEmpty(recipe.Tags) ? recipe.Tags : "No tags provided",
                Visibility = recipe.Visibility,
                Upvotes = recipe.Upvotes,
                Downvotes = recipe.Downvotes,
                CoverImages = recipe.CoverImages?.Select(img => Convert.ToBase64String(img.ImageData)).ToList() ?? new List<string>(),

                // 🔥 Fix: Ensure type consistency for Ingredients
                Ingredients = recipe.Ingredients != null && recipe.Ingredients.Any()
                    ? recipe.Ingredients.Select(i => new
                    {
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Name = i.Name
                    }).Cast<object>().ToList() // Ensures both cases return List<object>
                    : new List<object>(),

                // 🔥 Fix: Ensure type consistency for Instructions
                Instructions = recipe.Instructions != null && recipe.Instructions.Any()
                    ? recipe.Instructions.Select(instr => new
                    {
                        StepNumber = instr.StepNumber,
                        Step = instr.Step
                    }).Cast<object>().ToList() // Ensures both cases return List<object>
                    : new List<object>()
            };

            return Ok(formattedRecipe);
        }










        // 3. Create a Recipe
        [HttpPost]
        public async Task<IActionResult> CreateRecipe(
        [FromForm] Recipe recipe,
        [FromForm] List<IFormFile>? coverImages,
        [FromForm] List<IFormFile>? instructionImages,
        [FromForm] string? ingredients,
        [FromForm] string? instructions)
        {
            try
            {
                recipe.RecipeId = Guid.NewGuid();
                Console.WriteLine($"✅ Received Recipe Data: {System.Text.Json.JsonSerializer.Serialize(recipe)}");

                // ✅ Initialize Lists to Avoid Null Reference Issues
                recipe.Ingredients = new List<RecipeIngredient>();
                recipe.Instructions = new List<RecipeInstruction>();

                // ✅ Deserialize and Attach Ingredients
                if (!string.IsNullOrEmpty(ingredients))
                {
                    Console.WriteLine("🔹 Raw Ingredients JSON: " + ingredients);
                    try
                    {
                        var deserializedIngredients = System.Text.Json.JsonSerializer.Deserialize<List<RecipeIngredient>>(ingredients);
                        if (deserializedIngredients != null)
                        {
                            foreach (var ingredient in deserializedIngredients)
                            {
                                ingredient.IngredientId = Guid.NewGuid();
                                ingredient.RecipeId = recipe.RecipeId;
                            }
                            _context.RecipeIngredients.AddRange(deserializedIngredients); // ✅ Explicitly Save
                            Console.WriteLine("✅ Successfully saved ingredients");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error deserializing ingredients: " + ex.Message);
                    }
                }

                // ✅ Deserialize and Attach Instructions
                if (!string.IsNullOrEmpty(instructions))
                {
                    Console.WriteLine("🔹 Raw Instructions JSON: " + instructions);
                    try
                    {
                        var deserializedInstructions = System.Text.Json.JsonSerializer.Deserialize<List<RecipeInstruction>>(instructions);
                        if (deserializedInstructions != null)
                        {
                            for (int i = 0; i < deserializedInstructions.Count; i++)
                            {
                                deserializedInstructions[i].InstructionId = Guid.NewGuid();
                                deserializedInstructions[i].RecipeId = recipe.RecipeId;
                                deserializedInstructions[i].StepNumber = i + 1;

                                // ✅ Attach Image if Exists
                                if (instructionImages != null && i < instructionImages.Count)
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        await instructionImages[i].CopyToAsync(ms);
                                        deserializedInstructions[i].StepImage = ms.ToArray(); // ✅ Convert to byte[]
                                    }
                                }
                            }
                            _context.RecipeInstructions.AddRange(deserializedInstructions); // ✅ Explicitly Save
                            Console.WriteLine("✅ Successfully saved instructions");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error deserializing instructions: " + ex.Message);
                    }
                }

                // ✅ Save Cover Images
                recipe.CoverImages = new List<RecipeImage>();
                if (coverImages != null)
                {
                    foreach (var image in coverImages)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await image.CopyToAsync(ms);
                            recipe.CoverImages.Add(new RecipeImage
                            {
                                ImageId = Guid.NewGuid(),
                                ImageData = ms.ToArray(),
                                RecipeId = recipe.RecipeId
                            });
                        }
                    }
                }

                // ✅ Save Recipe First
                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                Console.WriteLine("✅ Recipe successfully saved to database!");
                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.RecipeId }, recipe);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error saving recipe:");
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An error occurred while creating the recipe.");
            }
        }









        // 4. Update a Recipe
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(Guid id, [FromForm] Recipe updatedRecipe, [FromForm] List<IFormFile>? coverImages, [FromForm] List<IFormFile>? instructionImages)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Include(r => r.CoverImages)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null) return NotFound("Recipe not found.");

            Console.WriteLine("Updating Recipe...");
            recipe.Name = updatedRecipe.Name;
            recipe.Description = updatedRecipe.Description;
            recipe.TimeTaken = updatedRecipe.TimeTaken;
            recipe.Tags = updatedRecipe.Tags;
            recipe.Visibility = updatedRecipe.Visibility;

            if (coverImages != null)
            {
                _context.RemoveRange(recipe.CoverImages);
                recipe.CoverImages.Clear();

                foreach (var image in coverImages)
                {
                    using (var ms = new MemoryStream())
                    {
                        await image.CopyToAsync(ms);
                        recipe.CoverImages.Add(new RecipeImage
                        {
                            ImageId = Guid.NewGuid(),
                            ImageData = ms.ToArray(),
                            RecipeId = recipe.RecipeId
                        });
                    }
                }
            }

            _context.RecipeIngredients.RemoveRange(recipe.Ingredients);
            foreach (var ingredient in updatedRecipe.Ingredients)
            {
                ingredient.IngredientId = Guid.NewGuid();
                ingredient.RecipeId = recipe.RecipeId;
                recipe.Ingredients.Add(ingredient);
            }

            _context.RecipeInstructions.RemoveRange(recipe.Instructions);
            for (int i = 0; i < updatedRecipe.Instructions.Count; i++)
            {
                var instruction = updatedRecipe.Instructions[i];
                instruction.InstructionId = Guid.NewGuid();
                instruction.RecipeId = recipe.RecipeId;

                if (instructionImages != null && i < instructionImages.Count)
                {
                    using (var ms = new MemoryStream())
                    {
                        await instructionImages[i].CopyToAsync(ms);
                        instruction.StepImage = ms.ToArray();
                    }
                }

                recipe.Instructions.Add(instruction);
            }

            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();

            Console.WriteLine("Recipe updated successfully.");
            return NoContent();
        }

        // 5. Delete a Recipe
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(Guid id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Include(r => r.CoverImages)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null) return NotFound("Recipe not found.");

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            Console.WriteLine("Recipe deleted successfully.");
            return NoContent();
        }
    }
}
