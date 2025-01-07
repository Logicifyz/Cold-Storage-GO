using Cold_Storage_GO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Cold_Storage_GO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NutritionalFactsController : ControllerBase
    {
        private readonly DbContexts _context;

        public NutritionalFactsController(DbContexts context)
        {
            _context = context;
        }

        // GET: api/NutritionalFacts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NutritionalFacts>>> GetNutritionalFacts()
        {
            return await _context.NutritionalFacts.Include(nf => nf.Dish).ToListAsync();
        }

        // GET: api/NutritionalFacts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<NutritionalFacts>> GetNutritionalFacts(Guid id)
        {
            var nutritionalFacts = await _context.NutritionalFacts
                .Include(nf => nf.Dish)
                .FirstOrDefaultAsync(nf => nf.DishId == id);

            if (nutritionalFacts == null)
                return NotFound();

            return nutritionalFacts;
        }

        // POST: api/NutritionalFacts
        [HttpPost]
        public async Task<ActionResult<NutritionalFacts>> CreateNutritionalFacts(NutritionalFacts nutritionalFacts)
        {
            if (!_context.Dishes.Any(d => d.DishId == nutritionalFacts.DishId))
                return BadRequest("DishId does not exist.");

            _context.NutritionalFacts.Add(nutritionalFacts);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNutritionalFacts), new { id = nutritionalFacts.DishId }, nutritionalFacts);
        }

        // PUT: api/NutritionalFacts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNutritionalFacts(Guid id, NutritionalFacts nutritionalFacts)
        {
            if (id != nutritionalFacts.DishId)
                return BadRequest();

            _context.Entry(nutritionalFacts).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.NutritionalFacts.Any(e => e.DishId == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/NutritionalFacts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNutritionalFacts(Guid id)
        {
            var nutritionalFacts = await _context.NutritionalFacts.FindAsync(id);
            if (nutritionalFacts == null)
                return NotFound();

            _context.NutritionalFacts.Remove(nutritionalFacts);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

}
