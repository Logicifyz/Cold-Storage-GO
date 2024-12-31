using System.ComponentModel.DataAnnotations;

namespace Cold_Storage_GO.Models
{
    public class UserProfile
    {
        [Key]
        public int ProfileId { get; set; } // Auto-incrementing primary key
        [Required]
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string StreetAddress { get; set; }
        public string PostalCode { get; set; }

        // Foreign key linking to User
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
