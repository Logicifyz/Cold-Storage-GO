using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Discussion
    {
        [Key]
        public Guid DiscussionId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Visibility { get; set; } = string.Empty;
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
    }
}
