using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class RecipeImage
    {
        [Key]
        public Guid ImageId { get; set; }

        [Required]
        public byte[] ImageData { get; set; }

        [Required]
        public Guid RecipeId { get; set; }

        [ForeignKey("RecipeId")]
        [JsonIgnore] // Prevent infinite recursion during serialization
        public Recipe Recipe { get; set; }
    }
}
