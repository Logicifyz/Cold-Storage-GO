using System.Collections.Generic;

namespace Cold_Storage_GO.Models
{
    public class RecommendationRequest
    {
        public Guid UserId { get; set; } // User ID
        public List<string> Ingredients { get; set; } = new(); // Ingredients to include
        public List<string> ExcludeIngredients { get; set; } = new(); // Ingredients to exclude
        public List<string> DietaryPreferences { get; set; } = new(); // Dietary preferences
        public int? MaxIngredients { get; set; } // Max number of ingredients
        public string Preference { get; set; } = string.Empty; // Cuisine preference
        public string FreeText { get; set; } = string.Empty; // Free-form text input
        public bool UseChat { get; set; } // Chat-based interaction
        public string CookingTime { get; set; } = string.Empty; // Cooking time constraint
    }
}
