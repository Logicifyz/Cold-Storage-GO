using System;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Redemptions
    {
        [Key]
        public Guid RedemptionId { get; set; } // Primary key

        [Required]
        public Guid UserId { get; set; } // Foreign key to the user

        [Required]
        public Guid RewardId { get; set; } // Foreign key to the reward

        [Required]
        public DateTime RedeemedAt { get; set; } // Timestamp when redeemed

        [Required]
        public DateTime ExpiryDate { get; set; } // Expiry date of the reward

        [Required]
        public bool RewardUsable { get; set; } // Whether the reward is still usable
    }
}
