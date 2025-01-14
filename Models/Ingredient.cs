using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Ingredient
    {
        [Key]
        public Guid IngredientId { get; set; } // Primary key

        [Required]
        [MaxLength(50)]
        public string Quantity { get; set; } = string.Empty; // e.g., "2"

        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty; // e.g., "pcs"

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty; // e.g., "Eggs"

        [Required]
        public Guid RecipeId { get; set; } // Foreign key
    }
}
