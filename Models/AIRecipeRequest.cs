using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class AIRecipeRequest
    {
        [Key] // ✅ Adding a primary key
        public Guid RequestId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        public List<string> Ingredients { get; set; } = new();
        public List<string> ExcludeIngredients { get; set; } = new();
        public List<string> DietaryPreferences { get; set; } = new();
        public int? MaxIngredients { get; set; }
        public string Preference { get; set; } = string.Empty;
        public string FreeText { get; set; } = string.Empty;
        public bool UseChat { get; set; }
        public string CookingTime { get; set; } = string.Empty;
        public int? Servings { get; set; }
        public bool NeedsClarification { get; set; } = false;
        public bool TrendingRequest { get; set; } = false;

        [Required]
        public string UserPrompt { get; set; } = string.Empty;
    }
}
