using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid(); // Auto-generate UUID
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string PasswordHash { get; set; } // Store hashed passwords
        [Required]
        public string Role { get; set; } = "user"; // Default role
        public UserProfile UserProfile { get; set; }
        public UserAdministration UserAdministration { get; set; }

    }
}
