using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class AIRecommendation
    {
        [Key]
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
