using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Instruction
    {
        [Key]
        public Guid InstructionId { get; set; } // Primary key

        [Required]
        public int StepNumber { get; set; } // Step number

        [Required]
        [MaxLength(2000)]
        public string Step { get; set; } = string.Empty; // Instruction description

        public string? MediaUrl { get; set; } // Optional media for the step

        [Required]
        public Guid RecipeId { get; set; } // Foreign key
    }
}
