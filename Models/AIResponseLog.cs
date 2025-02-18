using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public enum ResponseType
    {
        Recipe,
        FollowUp,
        TrendSuggestion,
        Redirect,
        Unknown
    }


    public class AIResponseLog
    {
        [Key]
        public Guid ChatId { get; set; }

        [Required]
        public Guid UserId { get; set; }


        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public ResponseType Type { get; set; } = ResponseType.Recipe;

        public string? UserResponse { get; set; }

        public Guid? FinalRecipeId { get; set; }

        public bool NeedsFinalDish { get; set; } = false;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserPrompt { get; set; } = string.Empty;

        [ForeignKey("FinalRecipeId")]
        [JsonIgnore]
        public FinalDish? FinalRecipe { get; set; }
    }

}
