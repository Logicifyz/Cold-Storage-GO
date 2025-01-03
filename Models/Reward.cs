using System;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Rewards
    {
        [Key]
        [Required]
        public Guid RewardId { get; set; } = Guid.NewGuid(); // Automatically generate RewardId

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Description { get; set; }

        [Required]
        public int CoinsCost { get; set; }

        [Required]
        [StringLength(100)]
        public string RewardType { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [StringLength(100)]
        public string AvailabilityStatus { get; set; }
    }
}
