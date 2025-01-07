using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class Wallet
    {
        [Key]
        public Guid WalletId { get; set; } // Primary key

        [Required]
        public Guid UserId { get; set; } // Foreign key to the user

        [Required]
        public int CoinsEarned { get; set; } // Total coins earned

        [Required]
        public int CoinsRedeemed { get; set; } // Total coins redeemed

        [NotMapped]
        public int CurrentBalance => CoinsEarned - CoinsRedeemed; // Calculated balance
    }
}
