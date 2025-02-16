using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class Comment
    {
        [Key]
        public Guid CommentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }  // ✅ UserId is required

        public Guid? RecipeId { get; set; }
        public Guid? DiscussionId { get; set; }

        [Required]
        [RegularExpression("^(recipe|discussion)$", ErrorMessage = "PostType must be 'recipe' or 'discussion'")]
        public string PostType { get; set; } = string.Empty;

        public Guid? ParentCommentId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Upvotes { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int Downvotes { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Relationships
        [JsonIgnore]
        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

        [ForeignKey("UserId")]
        [JsonIgnore]  // ✅ IGNORE User in API requests, only use for EF joins
        public virtual User? User { get; set; }

        [ForeignKey("RecipeId")]
        public virtual Recipe? Recipe { get; set; }

        [ForeignKey("DiscussionId")]
        public virtual Discussion? Discussion { get; set; }
    }
}
