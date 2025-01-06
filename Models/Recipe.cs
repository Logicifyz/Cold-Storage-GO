using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Recipe
    {
        [Key]
        public Guid RecipeId { get; set; }
        public Guid UserId { get; set; }
        public Guid DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TimeTaken { get; set; }
        public string Ingredients { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string MediaUrl { get; set; } = string.Empty;
        public string Visibility { get; set; } = string.Empty;
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
    }
}
