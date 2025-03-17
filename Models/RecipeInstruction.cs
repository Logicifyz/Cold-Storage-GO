using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace Cold_Storage_GO.Models
{
    public class RecipeInstruction
    {
        [Key]
        public Guid InstructionId { get; set; }
        [Required]
        public int StepNumber { get; set; }
        [Required]
        [MaxLength(2000)]
        public string Step { get; set; } = string.Empty;

        public byte[]? StepImage { get; set; }

        [Required]
        public Guid RecipeId { get; set; }

        [ForeignKey("RecipeId")]
        [JsonIgnore]
        public Recipe? Recipe { get; set; }
    }
}
