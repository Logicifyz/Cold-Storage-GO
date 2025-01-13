using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Recipe
    {
        [Key]
        public Guid RecipeId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid DishId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int TimeTaken { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Ingredients { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Instructions { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Tags { get; set; } = string.Empty;

        public List<string> MediaUrls { get; set; } = new List<string>(); // Updated to handle multiple media URLs

        [Required]
        [RegularExpression("^(public|private|friends-only)$", ErrorMessage = "Visibility must be 'public', 'private', or 'friends-only'")]
        public string Visibility { get; set; } = "public";

        [Range(0, int.MaxValue)]
        public int Upvotes { get; set; }

        [Range(0, int.MaxValue)]
        public int Downvotes { get; set; }
    }
}
