using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class RecipeIngredient
    {
        [Key]
        public Guid IngredientId { get; set; } 
        [Required]
        [MaxLength(50)]
        public string Quantity { get; set; } = string.Empty; 

        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty; 

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty; 

        [Required]
        public Guid RecipeId { get; set; } 

        [ForeignKey("RecipeId")]
        public Recipe? Recipe { get; set; }
    }
}
