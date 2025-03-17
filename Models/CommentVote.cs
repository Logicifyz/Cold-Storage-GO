using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class CommentVote
    {
        [Key]
        public Guid VoteId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CommentId { get; set; } // The comment being voted on

        [Required]
        public Guid UserId { get; set; } // The user who voted

        [Required]
        [Range(-1, 1, ErrorMessage = "Vote must be -1 (downvote), 0 (neutral), or 1 (upvote)")]
        public int VoteType { get; set; } // -1 = Downvote, 0 = Neutral, 1 = Upvote

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        // Relationships
        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]  // ✅ IGNORE User in API responses, only used for EF Core relationships
        public virtual User? User { get; set; }  // ✅ Make User optional
    }
}
