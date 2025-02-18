using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class AIRecipeImage
    {
        [Key]
        public Guid ImageId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid FinalDishId { get; set; } // ✅ Correct foreign key

        [ForeignKey("FinalDishId")]
        [JsonIgnore]
        public FinalDish? FinalDish { get; set; }

        [Required]
        public byte[] ImageData { get; set; } 

        [Required]
        public Guid UserId { get; set; } 

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
