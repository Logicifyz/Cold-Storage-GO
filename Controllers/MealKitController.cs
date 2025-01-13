using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class MealKitController : ControllerBase
    {
        private readonly DbContexts _context;

        public MealKitController(DbContexts context)
        {
            _context = context;
        }

        // GET: api/MealKit
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MealKit>>> GetMealKits()
        {
            return await _context.MealKits.ToListAsync();
        }

        // GET: api/MealKit/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MealKit>> GetMealKit(Guid id)
        {
            var mealKit = await _context.MealKits.FindAsync(id);

            if (mealKit == null)
            {
                return NotFound();
            }

            return mealKit;
        }

        // POST: api/MealKit
        [HttpPost]
        public async Task<ActionResult<MealKit>> CreateMealKit([FromForm] MealKitCreateRequest request)
        {
            byte[]? imageBytes = null;
            if (request.ListingImage != null)
            {
                using var memoryStream = new MemoryStream();
                await request.ListingImage.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var newMealKit = new MealKit
            {
                MealKitId = Guid.NewGuid(),
                DishIds = request.DishIds,
                Name = request.Name,
                Price = request.Price,
                ExpiryDate = request.ExpiryDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ListingImage = imageBytes,
                Tags = request.Tags,
                Ingredients = request.Ingredients
            };

            _context.MealKits.Add(newMealKit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMealKit), new { id = newMealKit.MealKitId }, newMealKit);
        }

        // PUT: api/MealKit/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMealKit(Guid id, [FromForm] MealKitUpdateRequest request)
        {
            var mealKit = await _context.MealKits.FindAsync(id);
            if (mealKit == null)
            {
                return NotFound();
            }

            mealKit.DishIds = request.DishIds;
            mealKit.Name = request.Name;
            mealKit.Price = request.Price;
            mealKit.ExpiryDate = request.ExpiryDate;
            mealKit.UpdatedAt = DateTime.UtcNow;
            mealKit.Tags = request.Tags;
            mealKit.Ingredients = request.Ingredients;

            if (request.ListingImage != null)
            {
                using var memoryStream = new MemoryStream();
                await request.ListingImage.CopyToAsync(memoryStream);
                mealKit.ListingImage = memoryStream.ToArray();
            }

            _context.Entry(mealKit).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MealKitExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/MealKit/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealKit(Guid id)
        {
            var mealKit = await _context.MealKits.FindAsync(id);
            if (mealKit == null)
            {
                return NotFound();
            }

            _context.MealKits.Remove(mealKit);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MealKitExists(Guid id)
        {
            return _context.MealKits.Any(e => e.MealKitId == id);
        }
    }

    public class MealKitCreateRequest
    {
        [Required(ErrorMessage = "DishIds are required.")]
        public List<Guid> DishIds { get; set; } = new();

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name length cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Price must be a positive number.")]
        public int Price { get; set; }

        [Required(ErrorMessage = "ExpiryDate is required.")]
        public DateTime ExpiryDate { get; set; }

        public IFormFile? ListingImage { get; set; }
        public List<string>? Tags { get; set; }

        [StringLength(1000, ErrorMessage = "Ingredients length cannot exceed 1000 characters.")]
        public string Ingredients { get; set; }
    }

    public class MealKitUpdateRequest
    {
        public List<Guid> DishIds { get; set; } = new();
        public string Name { get; set; }
        public int Price { get; set; }
        public DateTime ExpiryDate { get; set; }
        public IFormFile? ListingImage { get; set; }
        public List<string>? Tags { get; set; }

        [StringLength(1000, ErrorMessage = "Ingredients length cannot exceed 1000 characters.")]
        public string Ingredients { get; set; }
    }
}
