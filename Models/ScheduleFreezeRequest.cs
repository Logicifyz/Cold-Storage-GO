using System;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class ScheduleFreezeRequest
    {
        [Required]
        public DateTime StartDate { get; set; } // The date when the freeze should start
        public DateTime EndDate { get; set; } // The date when the freeze should start

    }
}
