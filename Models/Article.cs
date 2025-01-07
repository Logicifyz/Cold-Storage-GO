namespace Cold_Storage_GO.Models
{
    public class Article
    {
        public Guid ArticleId { get; set; } // Unique identifier for the article
        public string Title { get; set; } // Title of the article
        public string Content { get; set; } // Content of the article
        public string Category { get; set; } // Category of the article
        public DateTime CreatedAt { get; set; } // Creation date of the article
        public DateTime? UpdatedAt { get; set; } // Last update date (optional)

        // New fields
        public bool Highlighted { get; set; } // Indicates if the article is featured/highlighted
        public int Views { get; set; } // Tracks the number of views the article has received
        public Guid? StaffId { get; set; } // ID of the staff member who created or last updated the article
    }

}
