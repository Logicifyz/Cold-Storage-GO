using System.ComponentModel.DataAnnotations;

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
        [MaxLength(1000)] 
        public string Content { get; set; } = string.Empty;

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
    }
}
