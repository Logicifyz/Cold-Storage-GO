using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class RecommendationRequest
    {
        [Required]
        public Guid UserId { get; set; } // The user making the request

        public List<string>? Ingredients { get; set; } // Optional for chat-based interaction

        [MaxLength(50)]
        public string? Preference { get; set; } // Optional for chat-based interaction

        public bool UseChat { get; set; } = false; // If true, triggers chat-based interaction
    }
}
