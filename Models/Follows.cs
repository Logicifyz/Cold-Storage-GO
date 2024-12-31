using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class Follows
    {
        [Key, Column(Order = 0)]  // First part of the composite key
        public Guid FollowerId { get; set; }

        [Key, Column(Order = 1)]  // Second part of the composite key
        public Guid FollowedId { get; set; }

        // Navigation properties (no need for [ForeignKey] attributes here)
        public User Follower { get; set; }
        public User Followed { get; set; }
    }

}