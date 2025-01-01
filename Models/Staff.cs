using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class Staff
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid StaffId { get; set; } // Primary Key (UUID)

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // Staff Name

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } // Staff Email

        [Required]
        [MinLength(6)]
        public string Password { get; set; } // Encrypted Password

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } // Role (e.g., Support, Manager)

        [Required]
        public bool Status { get; set; } // Active (true) or Inactive (false)
    }
}
