using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class RewardsController : ControllerBase
    {
        private readonly DbContexts _context;

        public RewardsController(DbContexts context)
        {
            _context = context;
        }

        // GET: api/Rewards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rewards>>> GetRewards()
        {
            return await _context.Rewards.ToListAsync();
        }

        // GET: api/Rewards/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Rewards>> GetReward(Guid id)
        {
            var reward = await _context.Rewards.FindAsync(id);

            if (reward == null)
                return NotFound();

            return reward;
        }

        // POST: api/Rewards
        [HttpPost]
        public async Task<ActionResult<Rewards>> CreateReward(Rewards reward)
        {
            reward.RewardId = Guid.NewGuid(); // Automatically generate RewardId
            _context.Rewards.Add(reward);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReward), new { id = reward.RewardId }, reward);
        }

        // PUT: api/Rewards/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReward(Guid id, Rewards reward)
        {
            if (id != reward.RewardId)
                return BadRequest();

            _context.Entry(reward).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Rewards.Any(r => r.RewardId == id))
                    return NotFound();

                throw;
            }

            return NoContent();
        }

        // DELETE: api/Rewards/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReward(Guid id)
        {
            var reward = await _context.Rewards.FindAsync(id);
            if (reward == null)
                return NotFound();

            _context.Rewards.Remove(reward);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
