using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class FinalDish
    {
        [Key]
        public Guid DishId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public List<string> Ingredients { get; set; } = new();

        [Required]
        public List<string> Steps { get; set; } = new();

        public NutritionInfo Nutrition { get; set; } = new();

        public List<string> Tags { get; set; } = new();

        public int Servings { get; set; } = 1;

        public string CookingTime { get; set; } = string.Empty;

        public string Difficulty { get; set; } = "Medium";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsSaved { get; set; } = false; // ✅ Tracks saved status

        [JsonIgnore]
        public List<AIResponseLog>? AIResponses { get; set; } // ✅ Linked AI Responses

        [JsonIgnore]
        public List<AIRecipeImage>? AIImages { get; set; }
    }

    [Owned]
    public class NutritionInfo
    {
        public int Calories { get; set; } = 0;
        public int Protein { get; set; } = 0;
        public int Carbs { get; set; } = 0;
        public int Fats { get; set; } = 0;
    }
}
