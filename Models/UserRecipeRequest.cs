using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Models
{
    [Keyless]
    public class UserRecipeRequest
    {
        public Guid UserId { get; set; }
        public string FreeText { get; set; } = string.Empty; // Free-form user input

        public List<string> IngredientsInclude { get; set; } = new(); // Ingredients to include
        public List<string> IngredientsExclude { get; set; } = new(); // Ingredients to exclude
        public List<string> DietaryPreferences { get; set; } = new(); // Dietary preferences (e.g., Vegan)

        public int? MaxIngredients { get; set; } // Max number of ingredients

        public string Cuisine { get; set; } = string.Empty; // Preferred cuisine

        public string CookingTime { get; set; } = string.Empty; // Cooking time limit

        public int? Servings { get; set; } // Number of servings

        public bool NeedsClarification { get; set; } = false; // AI detects vague input & asks follow-up

        public bool TrendingRequest { get; set; } = false; // User is requesting a trending dish

        // Converts UserRecipeRequest to AIRecipeRequest for AI processing
        public AIRecipeRequest ToAIRecipeRequest(Guid userId)
        {
            return new AIRecipeRequest
            {
                UserId = userId,
                Ingredients = IngredientsInclude,
                ExcludeIngredients = IngredientsExclude,
                DietaryPreferences = DietaryPreferences,
                MaxIngredients = MaxIngredients,
                Preference = Cuisine,
                FreeText = FreeText,
                CookingTime = CookingTime,
                Servings = Servings,
                UseChat = string.IsNullOrWhiteSpace(FreeText), // If FreeText is empty, AI prompts for details
                NeedsClarification = NeedsClarification,
                TrendingRequest = TrendingRequest
            };
        }
    }
}