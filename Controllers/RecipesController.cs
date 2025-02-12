using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

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
                Name = !string.IsNullOrEmpty(recipe.Name) ? recipe.Name : "Untitled Recipe",
                Description = !string.IsNullOrEmpty(recipe.Description) ? recipe.Description : "No description available",
                TimeTaken = recipe.TimeTaken > 0 ? recipe.TimeTaken : 0,
                Tags = !string.IsNullOrEmpty(recipe.Tags) ? recipe.Tags : "No tags provided",
                Visibility = recipe.Visibility,
                Upvotes = recipe.Upvotes,
                Downvotes = recipe.Downvotes,
                CoverImages = recipe.CoverImages?.Select(img => Convert.ToBase64String(img.ImageData)).ToList() ?? new List<string>(),

                Ingredients = recipe.Ingredients?.Any() == true
                    ? recipe.Ingredients.Select(i => new
                    {
                        IngredientId = i.IngredientId,
                        Quantity = string.IsNullOrEmpty(i.Quantity) ? "Unknown Quantity" : i.Quantity,
                        Unit = string.IsNullOrEmpty(i.Unit) ? "Unknown Unit" : i.Unit,
                        Name = string.IsNullOrEmpty(i.Name) ? "Unnamed Ingredient" : i.Name
                    }).ToList<object>()  // ✅ Fix: Ensures both branches return `List<object>`
                    : new List<object>(), // ✅ Ensures consistent return type

                Instructions = recipe.Instructions?.Any() == true
                    ? recipe.Instructions.Select(instr => new
                    {
                        InstructionId = instr.InstructionId,
                        StepNumber = instr.StepNumber > 0 ? instr.StepNumber : 1,
                        Step = string.IsNullOrEmpty(instr.Step) ? "Step description missing" : instr.Step,
                        StepImage = instr.StepImage != null ? Convert.ToBase64String(instr.StepImage) : null
                    }).ToList<object>()  // ✅ Fix: Ensures both branches return `List<object>`
                    : new List<object>()  // ✅ Ensures consistent return type
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

                // ✅ Ensure Lists are Initialized
                recipe.Ingredients = new List<RecipeIngredient>();
                recipe.Instructions = new List<RecipeInstruction>();
                recipe.CoverImages = new List<RecipeImage>();

                // ✅ Deserialize & Attach Ingredients Using JsonDocument
                if (!string.IsNullOrEmpty(ingredients))
                {
                    Console.WriteLine("🔹 Raw Ingredients JSON: " + ingredients);
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(ingredients))
                        {
                            foreach (JsonElement element in doc.RootElement.EnumerateArray())
                            {
                                string quantity = element.GetProperty("quantity").GetString() ?? "[MISSING]";
                                string unit = element.GetProperty("unit").GetString() ?? "[MISSING]";
                                string name = element.GetProperty("name").GetString() ?? "[MISSING]";

                                var ingredient = new RecipeIngredient
                                {
                                    IngredientId = Guid.NewGuid(),
                                    RecipeId = recipe.RecipeId,
                                    Quantity = quantity,
                                    Unit = unit,
                                    Name = name,
                                    Recipe = recipe
                                };

                                recipe.Ingredients.Add(ingredient);
                                Console.WriteLine($"🔍 Ingredient Added: ID={ingredient.IngredientId}, Quantity={ingredient.Quantity}, Unit={ingredient.Unit}, Name={ingredient.Name}");
                            }
                        }
                        Console.WriteLine($"✅ Successfully parsed {recipe.Ingredients.Count} ingredients.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error parsing ingredients: " + ex.Message);
                    }
                }

                // ✅ Deserialize & Attach Instructions Using JsonDocument
                if (!string.IsNullOrEmpty(instructions))
                {
                    Console.WriteLine("🔹 Raw Instructions JSON: " + instructions);
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(instructions))
                        {
                            int stepNumber = 1;
                            foreach (JsonElement element in doc.RootElement.EnumerateArray())
                            {
                                string step = element.GetProperty("step").GetString() ?? "[STEP MISSING]";

                                var instruction = new RecipeInstruction
                                {
                                    InstructionId = Guid.NewGuid(),
                                    RecipeId = recipe.RecipeId,
                                    StepNumber = stepNumber++,
                                    Step = step,
                                    Recipe = recipe
                                };

                                recipe.Instructions.Add(instruction);
                                Console.WriteLine($"🔍 Instruction Added: ID={instruction.InstructionId}, StepNumber={instruction.StepNumber}, Step={instruction.Step}");
                            }
                        }
                        Console.WriteLine($"✅ Successfully parsed {recipe.Instructions.Count} instructions.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error parsing instructions: " + ex.Message);
                    }
                }

                // ✅ Save Step Images
                if (instructionImages != null && instructionImages.Count > 0)
                {
                    for (int i = 0; i < instructionImages.Count; i++)
                    {
                        if (i < recipe.Instructions.Count && instructionImages[i] != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                await instructionImages[i].CopyToAsync(ms);
                                recipe.Instructions[i].StepImage = ms.ToArray();
                                Console.WriteLine($"📸 Step Image Saved for Instruction ID={recipe.Instructions[i].InstructionId}");
                            }
                        }
                    }
                }

                // ✅ Save Cover Images
                if (coverImages != null)
                {
                    foreach (var image in coverImages)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await image.CopyToAsync(ms);
                            var recipeImage = new RecipeImage
                            {
                                ImageId = Guid.NewGuid(),
                                ImageData = ms.ToArray(),
                                RecipeId = recipe.RecipeId,
                                Recipe = recipe
                            };
                            recipe.CoverImages.Add(recipeImage);
                        }
                    }
                }

                // ✅ Attach Related Entities to the DbContext
                _context.Recipes.Add(recipe);
                _context.RecipeIngredients.AddRange(recipe.Ingredients);
                _context.RecipeInstructions.AddRange(recipe.Instructions);
                _context.RecipeImages.AddRange(recipe.CoverImages);

                Console.WriteLine("🔥 Final Check Before Save:");
                Console.WriteLine($"📌 Recipe ID: {recipe.RecipeId}");
                Console.WriteLine($"📌 Tracked Ingredients Count Before Save: {recipe.Ingredients.Count}");
                Console.WriteLine($"📌 Tracked Instructions Count Before Save: {recipe.Instructions.Count}");

                await _context.SaveChangesAsync();

                Console.WriteLine("✅ Recipe & Related Data Successfully Saved to Database!");
                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.RecipeId }, recipe);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error saving recipe:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
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
