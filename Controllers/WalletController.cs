using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cold_Storage_GO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly DbContexts _context;

        public WalletController(DbContexts context)
        {
            _context = context;
        }

        // POST: api/Wallet/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet(Guid userId)
        {
            // Check if the user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // Check if the wallet already exists for the user
            if (await _context.Wallets.AnyAsync(w => w.UserId == userId))
                return BadRequest("Wallet already exists for the user.");

            // Create wallet with default values
            var wallet = new Wallet
            {
                WalletId = Guid.NewGuid(),
                UserId = userId,
                CoinsEarned = 0,
                CoinsRedeemed = 0
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWallet", new { userId = userId }, wallet);
        }


        // GET: api/Wallet/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<Wallet>> GetWallet(Guid userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return NotFound();

            return wallet;
        }

        // POST: api/Wallet/earn
        [HttpPost("earn")]
        public async Task<IActionResult> EarnCoins(Guid userId, int coins)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return NotFound();

            wallet.CoinsEarned += coins;
            await _context.SaveChangesAsync();

            return Ok(wallet);
        }

        // POST: api/Wallet/deduct
        [HttpPost("deduct")]
        public async Task<IActionResult> DeductCoins(Guid userId, int coins)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return NotFound();

            if (wallet.CurrentBalance < coins)
                return BadRequest("Insufficient balance.");

            wallet.CoinsRedeemed += coins;
            await _context.SaveChangesAsync();

            return Ok(wallet);
        }

        // POST: api/Wallet/redeem
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemReward(Guid userId, Guid rewardId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            var reward = await _context.Rewards.FindAsync(rewardId);

            if (wallet == null || reward == null)
                return NotFound();

            if (wallet.CurrentBalance < reward.CoinsCost)
                return BadRequest("Insufficient balance to redeem reward.");

            // Deduct coins
            wallet.CoinsRedeemed += reward.CoinsCost;

            // Create redemption record
            var redemption = new Redemptions
            {
                RedemptionId = Guid.NewGuid(),
                UserId = userId,
                RewardId = rewardId,
                RedeemedAt = DateTime.UtcNow,
                ExpiryDate = reward.ExpiryDate,
                RewardUsable = true
            };

            _context.Redemptions.Add(redemption);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWallet", new { userId = userId }, redemption);
        }
    }
}
