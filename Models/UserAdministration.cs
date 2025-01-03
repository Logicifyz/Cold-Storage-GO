using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class UserAdministration
    {
        [Key]
        [ForeignKey("User")]
        public Guid UserId { get; set; } // Primary Key and Foreign Key to Users table

        public bool Verified { get; set; } = false; // Indicates if the email is verified

        public string? VerificationToken { get; set; }
        // Token for password reset
        public string? PasswordResetToken { get; set; }

        public bool Activation { get; set; } = true; // Indicates if the account is active

        public int FailedLoginAttempts { get; set; } = 0; // Tracks the number of failed login attempts

        public DateTime? LockoutUntil { get; set; } // Indicates until when the account is locked
        public DateTime? LastFailedLogin { get; set; } // New property to track the last failed login timestamp


        // Navigation Property
        public User User { get; set; } // Link to the User entity
    }
}
