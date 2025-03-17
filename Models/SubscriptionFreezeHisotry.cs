using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class SubscriptionFreezeHistory
    {
        [Key]
        public Guid FreezeId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SubscriptionId { get; set; }

        [Required]
        public DateTime FreezeStartDate { get; set; }

        public DateTime? FreezeEndDate { get; set; } // Nullable until unfreezing

        [ForeignKey("SubscriptionId")]
        public Subscription Subscription { get; set; }
    }
}
