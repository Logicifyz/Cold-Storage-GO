using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cold_Storage_GO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class DishController : ControllerBase
    {
        private readonly DbContexts _context;

        public DishController(DbContexts context)
        {
            _context = context;
        }

        // GET: api/Dish
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Dish>>> GetDishes()
        {
            return await _context.Dishes.ToListAsync();
        }

        // GET: api/Dish/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Dish>> GetDish(Guid id)
        {
            var dish = await _context.Dishes.FindAsync(id);
            if (dish == null)
                return NotFound();

            return dish;
        }

        // POST: api/Dish
        [HttpPost]
        public async Task<ActionResult<Dish>> CreateDish([FromBody] Dish dish)
        {
            if (dish == null)
            {
                return BadRequest("Dish data is required.");
            }

            dish.DishId = Guid.NewGuid(); // Auto-generate DishId
            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDish), new { id = dish.DishId }, dish);
        }

        // PUT: api/Dish/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDish(Guid id, [FromBody] Dish dish)
        {
            if (id != dish.DishId)
                return BadRequest("Dish ID in the URL and request body must match.");

            _context.Entry(dish).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Dishes.Any(e => e.DishId == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Dish/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDish(Guid id)
        {
            var dish = await _context.Dishes.FindAsync(id);
            if (dish == null)
                return NotFound();

            _context.Dishes.Remove(dish);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}