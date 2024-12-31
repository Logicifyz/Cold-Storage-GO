using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;

namespace Cold_Storage_GO.Controllers
{
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
        public async Task<ActionResult<MealKit>> CreateMealKit(MealKit mealKit)
        {
            var newMealKit = new MealKit
            {
                MealKitId = Guid.NewGuid(),
                DishId = mealKit.DishId,
                Name = mealKit.Name,
                Price = mealKit.Price,
                ExpiryDate = mealKit.ExpiryDate.Date // Only include date in dd/MM/yyyy format
            };

            _context.MealKits.Add(newMealKit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMealKit), new { id = newMealKit.MealKitId }, newMealKit);
        }


        // PUT: api/MealKit/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMealKit(Guid id, MealKit mealKit)
        {
            if (id != mealKit.MealKitId)
            {
                return BadRequest();
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
}
