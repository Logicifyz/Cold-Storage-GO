using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Comment
    {
        [Key]
        public Guid CommentId { get; set; }

        [Required]
        public Guid UserId { get; set; } 

        public Guid? RecipeId { get; set; } 

        public Guid? DiscussionId { get; set; } 

        [Required]
        [RegularExpression("^(recipe|discussion)$", ErrorMessage = "PostType must be 'recipe' or 'discussion'")]
        public string PostType { get; set; } = string.Empty; 

        public Guid? ParentCommentId { get; set; } 

        [Required]
        [MaxLength(500)] 
        public string Content { get; set; } = string.Empty;

        [Range(0, int.MaxValue)] 
        public int Upvotes { get; set; }

        [Range(0, int.MaxValue)] 
        public int Downvotes { get; set; }

        
        public Comment? ParentComment { get; set; }

        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
