﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class UserProfile
    {
        [Key]
        public Guid ProfileId { get; set; } = Guid.NewGuid(); // Auto-generate UUID
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string StreetAddress { get; set; }
        public string PostalCode { get; set; }

        // Foreign key linking to User
        public Guid UserId { get; set; }
        [JsonIgnore] // Ignore this property during serialization

        public User User { get; set; }
        // Subscription status
        [Required]
        public string SubscriptionStatus { get; set; } = "Inactive"; // Default value
        public byte[]? ProfilePicture { get; set; }  // Stores the profile picture as a Blob (byte array)


    }
}
