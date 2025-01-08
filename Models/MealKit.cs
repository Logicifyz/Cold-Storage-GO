using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cold_Storage_GO.Models
{
    public class MealKit
    {
        public Guid MealKitId { get; set; }
        public Guid DishId { get; set; }

        [Required, MinLength(3), MaxLength(100)]
        public string Name { get; set; }
        public int Price { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime ExpiryDate { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; }
        public byte[]? ListingImage { get; set; }  // Stores the Image as a Blob (byte array)
    }
}