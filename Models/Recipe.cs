using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

        public List<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
        public List<RecipeInstruction> Instructions { get; set; } = new List<RecipeInstruction>();

        [MaxLength(200)]
        public string Tags { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(public|private|friends-only)$", ErrorMessage = "Visibility must be 'public', 'private', or 'friends-only'")]
        public string Visibility { get; set; } = "public";

        [Range(0, int.MaxValue)]
        public int Upvotes { get; set; }

        [Range(0, int.MaxValue)]
        public int Downvotes { get; set; }

        [JsonIgnore] // Prevent serialization issues
        public List<RecipeImage> CoverImages { get; set; } = new List<RecipeImage>();

    }
}
