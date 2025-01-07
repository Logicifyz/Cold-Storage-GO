using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class RecommendationRequest
    {
        [Required]
        public Guid UserId { get; set; } 

        public List<string>? Ingredients { get; set; } 

        [MaxLength(50)]
        public string? Preference { get; set; } 

        public bool UseChat { get; set; } = false; 
    }
}
