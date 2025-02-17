using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class DiscussionVote
    {
        [Key]
        public Guid VoteId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid DiscussionId { get; set; }

        [Required]
        [Range(-1, 1)]
        public int VoteType { get; set; }

        [JsonIgnore] 
        public Discussion Discussion { get; set; }
 
    }
}