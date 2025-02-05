using System.Collections.Generic;

namespace Cold_Storage_GO.Models
{
    public class AdvancedControls
    {
        public string FreeText { get; set; } = string.Empty; // Free-form user input
        public List<string> IngredientsInclude { get; set; } = new(); // Ingredients to include
        public List<string> IngredientsExclude { get; set; } = new(); // Ingredients to exclude
        public List<string> DietaryPreferences { get; set; } = new(); // Dietary preferences (e.g., Vegan)
        public int? MaxIngredients { get; set; } // Max number of ingredients
        public string Cuisine { get; set; } = string.Empty; // Preferred cuisine
        public string CookingTime { get; set; } = string.Empty; // Cooking time limit

        // Converts AdvancedControls to a unified RecommendationRequest model
        public RecommendationRequest ToRecommendationRequest(Guid userId)
        {
            return new RecommendationRequest
            {
                UserId = userId,
                Ingredients = IngredientsInclude,
                ExcludeIngredients = IngredientsExclude,
                DietaryPreferences = DietaryPreferences,
                MaxIngredients = MaxIngredients,
                Preference = Cuisine,
                FreeText = FreeText,
                UseChat = string.IsNullOrWhiteSpace(FreeText) // Use chat if FreeText is blank
            };
        }
    }
}
