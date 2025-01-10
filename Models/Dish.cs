using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class Dish
    {
        [Key]
        public Guid DishId { get; set; } = Guid.NewGuid(); // Automatically generated

        public Guid? UserId { get; set; } // Nullable foreign key

        public Guid? MealKitId { get; set; } // Nullable foreign key

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // Required

        [Required]
        [StringLength(100)]
        public string Ingredients { get; set; } // Required

        [Required]
        [StringLength(100)]
        public string Instructions { get; set; } // Required

     
    }

    
    public class NutritionalFacts
    {
        [Key]
        [Required]
        public Guid DishId { get; set; } // Foreign key to Dish

        // Remove the requirement for Dish in the body by making it optional
        [ForeignKey("DishId")]
        public Dish? Dish { get; set; } // Optional navigation property

        [Required]
        [StringLength(100)]
        public string DietaryCategory { get; set; }

        [Required]
        public int Calories { get; set; }

        [Required]
        public int SaturatedFat { get; set; }

        [Required]
        public int TransFat { get; set; }

        [Required]
        public int Cholesterol { get; set; }

        [Required]
        public int Sodium { get; set; }

        [Required]
        public int DietaryFibre { get; set; }

        [Required]
        public int Sugar { get; set; }

        [Required]
        public int Protein { get; set; }

        [StringLength(100)]
        public string? Vitamins { get; set; } // Optional field

        [StringLength(100)]
        public string? Ingredients { get; set; } // Optional field
    }
}

