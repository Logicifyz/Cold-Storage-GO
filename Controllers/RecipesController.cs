using Microsoft.AspNetCore.Mvc;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Newtonsoft.Json;

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
            var sessionId = Request.Cookies["SessionId"];
            Guid? userId = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
                if (userSession != null)
                    userId = userSession.UserId;
            }

            var query = from recipe in _context.Recipes
                        join user in _context.Users on recipe.UserId equals user.UserId
                        join userProfile in _context.UserProfiles on user.UserId equals userProfile.UserId
                        select new
                        {
                            recipe.RecipeId,
                            recipe.UserId,
                            User = new
                            {
                                user.Username,
                                userProfile.ProfilePicture
                            },
                            recipe.DishId,
                            recipe.Name,
                            recipe.Description,
                            recipe.TimeTaken,
                            recipe.Tags,
                            recipe.Visibility,
                            recipe.Upvotes,
                            recipe.Downvotes,
                            CoverImages = recipe.CoverImages != null
                                ? recipe.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                                : new List<string>(),
                            Votes = recipe.Votes
                        };

            var recipes = await query.ToListAsync();

            var formattedRecipes = recipes.Select(recipe => new
            {
                recipe.RecipeId,
                recipe.UserId,
                User = recipe.User,
                recipe.DishId,
                recipe.Name,
                recipe.Description,
                recipe.TimeTaken,
                recipe.Tags,
                recipe.Visibility,
                recipe.Upvotes,
                recipe.Downvotes,
                userVote = userId.HasValue
                    ? recipe.Votes.FirstOrDefault(v => v.UserId == userId)?.VoteType ?? 0
                    : 0,
                recipe.CoverImages
            });

            return Ok(formattedRecipes);
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipe(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Recipe ID is missing.");
            }

            var sessionId = Request.Cookies["SessionId"];
            Guid? userId = null;

            if (!string.IsNullOrEmpty(sessionId))
            {
                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
                if (userSession != null)
                    userId = userSession.UserId;
            }

            // ✅ Fetch Recipe + User Data + Dish Name (MealKit Used)
            var query = from recipe in _context.Recipes
                        join user in _context.Users on recipe.UserId equals user.UserId
                        join userProfile in _context.UserProfiles on user.UserId equals userProfile.UserId
                        join dish in _context.Dishes on recipe.DishId equals dish.DishId into dishJoin
                        from dish in dishJoin.DefaultIfEmpty() // ✅ Ensures the recipe still loads if there's no associated dish
                        where recipe.RecipeId == id
                        select new
                        {
                            recipe.RecipeId,
                            recipe.UserId,
                            User = new
                            {
                                user.Username,
                                userProfile.ProfilePicture
                            },
                            recipe.DishId,
                            DishName = dish != null ? dish.Name : "Unknown MealKit", // ✅ Extract Dish Name
                            recipe.Name,
                            recipe.Description,
                            recipe.TimeTaken,
                            recipe.Tags,
                            recipe.Visibility,
                            recipe.Upvotes,
                            recipe.Downvotes,
                            CoverImages = recipe.CoverImages != null
                                ? recipe.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                                : new List<string>(),
                            Votes = recipe.Votes,
                            Ingredients = recipe.Ingredients.Select(i => new
                            {
                                i.IngredientId,
                                Quantity = string.IsNullOrEmpty(i.Quantity) ? "Unknown Quantity" : i.Quantity,
                                Unit = string.IsNullOrEmpty(i.Unit) ? "Unknown Unit" : i.Unit,
                                Name = string.IsNullOrEmpty(i.Name) ? "Unnamed Ingredient" : i.Name
                            }).ToList(),
                            Instructions = recipe.Instructions.Select(instr => new
                            {
                                instr.InstructionId,
                                StepNumber = instr.StepNumber > 0 ? instr.StepNumber : 1,
                                Step = string.IsNullOrEmpty(instr.Step) ? "Step description missing" : instr.Step,
                                StepImage = instr.StepImage != null ? Convert.ToBase64String(instr.StepImage) : null
                            }).ToList()
                        };

            var recipeData = await query.FirstOrDefaultAsync();

            if (recipeData == null)
            {
                return NotFound("Recipe not found.");
            }

            var formattedRecipe = new
            {
                recipeData.RecipeId,
                recipeData.UserId,
                User = recipeData.User, // ✅ Includes username and profile picture
                recipeData.DishId,
                recipeData.DishName, // ✅ Includes Dish Name (MealKit Used)
                recipeData.Name,
                recipeData.Description,
                recipeData.TimeTaken,
                recipeData.Tags,
                recipeData.Visibility,
                recipeData.Upvotes,
                recipeData.Downvotes,
                UserVote = userId.HasValue
                    ? recipeData.Votes.FirstOrDefault(v => v.UserId == userId)?.VoteType ?? 0
                    : 0,
                recipeData.CoverImages,
                recipeData.Ingredients,
                recipeData.Instructions
            };

            return Ok(formattedRecipe);
        }



        [HttpGet("my-recipes")]
        public async Task<IActionResult> GetMyRecipes()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized("Session not found.");

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null)
                return Unauthorized("Invalid session.");

            var recipes = await _context.Recipes
                .Where(r => r.UserId == userSession.UserId)
                .Select(recipe => new
                {
                    recipe.RecipeId,
                    recipe.Name,
                    recipe.Description,
                    recipe.Visibility,  // ✅ Includes visibility for private/public logic
                    recipe.Upvotes,
                    recipe.Downvotes,
                    CoverImages = recipe.CoverImages != null
                        ? recipe.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                        : new List<string>() // ✅ Prevents null errors
                })
                .ToListAsync();

            return Ok(recipes);
        }


        [HttpPost("{id}/vote")]
        public async Task<IActionResult> VoteRecipe(Guid id, [FromBody] int voteType)
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

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound("Recipe not found.");

            var existingVote = await _context.RecipeVotes
                .FirstOrDefaultAsync(v => v.RecipeId == id && v.UserId == userId);

            if (existingVote != null)
            {
                if (existingVote.VoteType == voteType)
                {
                    // ✅ Remove vote if clicked again
                    _context.RecipeVotes.Remove(existingVote);

                    // ✅ Adjust the vote counts
                    if (voteType == 1)
                        recipe.Upvotes = Math.Max(0, recipe.Upvotes - 1); // Decrease upvotes
                    else if (voteType == -1)
                        recipe.Downvotes = Math.Max(0, recipe.Downvotes - 1); // Decrease downvotes
                }
                else
                {
                    // ✅ Change vote (Upvote <-> Downvote)
                    if (existingVote.VoteType == 1)
                    {
                        recipe.Upvotes = Math.Max(0, recipe.Upvotes - 1); // Decrease upvotes
                        recipe.Downvotes += 1; // Increase downvotes
                    }
                    else if (existingVote.VoteType == -1)
                    {
                        recipe.Downvotes = Math.Max(0, recipe.Downvotes - 1); // Decrease downvotes
                        recipe.Upvotes += 1; // Increase upvotes
                    }

                    existingVote.VoteType = voteType; // Update the vote type
                    _context.RecipeVotes.Update(existingVote);
                }
            }
            else
            {
                // ✅ Add new vote
                var newVote = new RecipeVote
                {
                    VoteId = Guid.NewGuid(),
                    RecipeId = id,
                    UserId = userId,
                    VoteType = voteType
                };
                _context.RecipeVotes.Add(newVote);

                // ✅ Adjust the vote counts
                if (voteType == 1)
                    recipe.Upvotes += 1; // Increase upvotes
                else if (voteType == -1)
                    recipe.Downvotes += 1; // Increase downvotes
            }

            await _context.SaveChangesAsync();

            var userVote = await _context.RecipeVotes
               .Where(v => v.RecipeId == id && v.UserId == userId)
               .Select(v => v.VoteType)
               .FirstOrDefaultAsync();

            return Ok(new
            {
                upvotes = recipe.Upvotes,
                downvotes = recipe.Downvotes,
                voteScore = recipe.Upvotes - recipe.Downvotes,
                userVote = userVote
            });
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
            Console.WriteLine($"📌 Received Recipe Creation Request");
            Console.WriteLine($"📌 DishId: {recipe.DishId}");
            Console.WriteLine($"📌 UserId: {recipe.UserId}");

            if (recipe.DishId == Guid.Empty)
            {
                Console.WriteLine("❌ ERROR: DishId is missing!");
                return BadRequest(new { message = "A valid MealKit (Dish) must be selected." });
            }

            try
            {
                // 🔹 Retrieve User ID from Session
                var sessionId = Request.Cookies["SessionId"];
                if (string.IsNullOrEmpty(sessionId))
                {
                    return Unauthorized(new { message = "User is not logged in." });
                }

                var userSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

                if (userSession == null)
                {
                    return Unauthorized(new { message = "Invalid or expired session." });
                }

                Console.WriteLine($"📌 Received Dish ID: {recipe.DishId}");  // Log received DishId

                if (recipe.DishId == Guid.Empty)
                {
                    Console.WriteLine("❌ DishId is missing in request!");
                    return BadRequest(new { message = "A valid MealKit (Dish) must be selected." });
                }

                Console.WriteLine($"✅ Received DishId: {recipe.DishId}");

               

                // 🔹 Associate Recipe with Logged-In User
                recipe.RecipeId = Guid.NewGuid();
                recipe.UserId = userSession.UserId;
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

                await _context.SaveChangesAsync();

                Console.WriteLine("🔥 Final Check Before Save:");
                Console.WriteLine($"📌 Recipe ID: {recipe.RecipeId}");
                Console.WriteLine($"📌 Tracked Ingredients Count Before Save: {recipe.Ingredients.Count}");
                Console.WriteLine($"📌 Tracked Instructions Count Before Save: {recipe.Instructions.Count}");
                Console.WriteLine("✅ Recipe & Related Data Successfully Saved to Database!");
                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.RecipeId }, recipe);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error after saving recipe: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred after saving the recipe.", details = ex.Message });
            }
        }

        [HttpGet("user/{username}")]
        public async Task<IActionResult> GetUserRecipes(string username)
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

            var recipes = await _context.Recipes
                .Where(r => r.UserId == user.UserId)
                .Select(recipe => new
                {
                    recipe.RecipeId,
                    recipe.Name,
                    recipe.Description,
                    recipe.Upvotes,
                    recipe.Downvotes,
                    CoverImages = recipe.CoverImages != null
                        ? recipe.CoverImages.Select(img => Convert.ToBase64String(img.ImageData)).ToList()
                        : new List<string>()  // Ensures no null errors
                })
                .ToListAsync();

            return Ok(recipes);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(Guid id,
        [FromForm] string name,
        [FromForm] string description,
        [FromForm] string timeTaken,  // 🛠️ Fix conversion issue
        [FromForm] string tags,
        [FromForm] string visibility,
        [FromForm] string? ingredients,
        [FromForm] string? instructions,
        [FromForm] List<IFormFile>? coverImages)
        {
            if (id == Guid.Empty) return BadRequest("Invalid Recipe ID.");

            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId)) return Unauthorized("User is not logged in.");

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null) return Unauthorized("Invalid or expired session.");

            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .Include(r => r.CoverImages)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null) return NotFound("Recipe not found.");

            // 🔒 Ensure user owns the recipe
            if (recipe.UserId != userSession.UserId) return Forbid("You do not have permission to edit this recipe.");

            // ✅ Convert timeTaken from string to int
            if (int.TryParse(timeTaken, out int parsedTimeTaken))
            {
                recipe.TimeTaken = parsedTimeTaken;
            }
            else
            {
                return BadRequest("Invalid timeTaken value. Must be an integer.");
            }

            // ✅ Update basic recipe details
            recipe.Name = name;
            recipe.Description = description;
            recipe.Tags = tags;
            recipe.Visibility = visibility;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Recipe updated successfully!", RecipeId = recipe.RecipeId });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(Guid id)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId)) return Unauthorized("Session not found.");

            var userSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);

            if (userSession == null) return Unauthorized("Invalid session.");

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return NotFound("Recipe not found.");

            // ✅ Ensure user owns the recipe
            if (recipe.UserId != userSession.UserId)
            {
                return Forbid("You do not have permission to delete this recipe.");
            }

            // ✅ Query and delete related comments separately (to avoid foreign key constraint issues)
            var relatedComments = await _context.Comments.Where(c => c.RecipeId == id).ToListAsync();
            _context.Comments.RemoveRange(relatedComments);

            // ✅ Now delete the recipe
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Recipe deleted successfully" });
        }





    }
}