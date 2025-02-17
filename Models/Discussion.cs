using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class Discussion
    {
        [Key]
        public Guid DiscussionId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)] // Increased size to allow rich text
        public string Content { get; set; } = string.Empty; // Stores rich text (HTML)

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(public|private|friends-only)$", ErrorMessage = "Visibility must be 'public', 'private', or 'friends-only'")]
        public string Visibility { get; set; } = "public";

        [Range(0, int.MaxValue)]
        public int Upvotes { get; set; }

        [Range(0, int.MaxValue)]
        public int Downvotes { get; set; }

        public List<DiscussionImage>? CoverImages { get; set; }

        [JsonIgnore] 
        public List<DiscussionVote> Votes { get; set; } = new List<DiscussionVote>();
    }
}
