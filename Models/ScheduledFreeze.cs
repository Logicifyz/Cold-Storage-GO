using System;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class ScheduledFreeze
    {
        [Key]
        public Guid ScheduledFreezeId { get; set; }

        [Required]
        public Guid SubscriptionId { get; set; }

        [Required]
        public DateTime FreezeStartDate { get; set; }
    }
}
