using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cold_Storage_GO.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Cold_Storage_GO.Controllers
{
    [AllowAnonymous]
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
        public async Task<IActionResult> CreateWallet()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("User is not logged in.");
            }

            var userSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
            {
                return Unauthorized("User session is invalid or has expired.");
            }

            var userId = userSession.UserId;

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

        // GET: api/Wallet
        [HttpGet]
        public async Task<ActionResult<Wallet>> GetWallet()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("User is not logged in.");
            }

            var userSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
            {
                return Unauthorized("User session is invalid or has expired.");
            }

            var userId = userSession.UserId;

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return NotFound();

            return wallet;
        }

        // POST: api/Wallet/earn
        [HttpPost("earn")]
        public async Task<IActionResult> EarnCoins([FromBody] EarnCoinsRequest request)
        {
            if (request == null || request.Coins <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
            if (wallet == null)
                return NotFound("Wallet not found.");

            wallet.CoinsEarned += request.Coins;



            await _context.SaveChangesAsync();

            return Ok(wallet);
        }

        // POST: api/Wallet/deduct
        [HttpPost("deduct")]
        public async Task<IActionResult> DeductCoins([FromBody] DeductCoinsRequest request)
        {
            if (request == null || request.Coins <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);

            if (wallet == null)
                return NotFound("Wallet not found.");

            if (wallet.CurrentBalance < request.Coins)
                return BadRequest("Insufficient balance.");

            wallet.CoinsRedeemed += request.Coins;
            await _context.SaveChangesAsync();

            return Ok(wallet);
        }

        public class EarnCoinsRequest
        {
            public Guid UserId { get; set; } // Changed from int to Guid
            public int Coins { get; set; }
        }

        public class DeductCoinsRequest
        {
            public Guid UserId { get; set; } // Changed from int to Guid
            public int Coins { get; set; }
        }


        // POST: api/Wallet/redeem
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemReward(Guid rewardId)
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("User is not logged in.");
            }

            var userSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
            {
                return Unauthorized("User session is invalid or has expired.");
            }

            var userId = userSession.UserId;

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
            var redemptionEvent = new RewardRedemptionEvent
            {
                RedemptionId = redemption.RedemptionId,
                UserId = userId,
                RewardId = rewardId,
                RedeemedAt = DateTime.UtcNow,
                ExpiryDate = reward.ExpiryDate,
                RewardUsable = true
            };
      

            _context.RewardRedemptionEvents.Add(redemptionEvent);       
            _context.Redemptions.Add(redemption);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWallet", new { userId = userId }, redemption);
        }

        // GET: api/Wallet/redemptions
        [HttpGet("redemptions")]
        public async Task<IActionResult> GetUserRedemptions()
        {
            var sessionId = Request.Cookies["SessionId"];
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("User is not logged in.");
            }

            // Validate the session
            var userSession = await _context.UserSessions.FirstOrDefaultAsync(s => s.UserSessionId == sessionId && s.IsActive);
            if (userSession == null)
            {
                return Unauthorized("User session is invalid or has expired.");
            }

            var userId = userSession.UserId;

            // Query the redemptions for the user
            var redemptions = await _context.Redemptions
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.RedemptionId,
                    r.RewardId,
                    r.RedeemedAt,
                    r.ExpiryDate,
                    r.RewardUsable
                })
                .ToListAsync();

            // If no redemptions are found
            if (redemptions == null || !redemptions.Any())
            {
                return NotFound("No redemptions found for the user.");
            }

            return Ok(redemptions);
        }


    }
}
