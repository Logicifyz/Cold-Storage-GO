using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class AIRecommendation
    {
        [Key]
        public Guid ChatId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
