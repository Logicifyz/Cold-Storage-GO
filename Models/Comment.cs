using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Comment
    {
        [Key]
        public Guid CommentId { get; set; }
        public Guid UserId { get; set; }
        public Guid RecipeId { get; set; }
        public Guid? DiscussionId { get; set; }
        public string PostType { get; set; } = string.Empty;
        public Guid? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }

        // Navigation property for threading
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
