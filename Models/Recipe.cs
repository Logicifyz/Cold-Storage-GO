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

        public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

        public List<Instruction> Instructions { get; set; } = new List<Instruction>();

        [MaxLength(200)]
        public string Tags { get; set; } = string.Empty;

        public List<string> MediaUrls { get; set; } = new List<string>();

        [Required]
        [RegularExpression("^(public|private|friends-only)$", ErrorMessage = "Visibility must be 'public', 'private', or 'friends-only'")]
        public string Visibility { get; set; } = "public";

        [Range(0, int.MaxValue)]
        public int Upvotes { get; set; }

        [Range(0, int.MaxValue)]
        public int Downvotes { get; set; }
    }
}
