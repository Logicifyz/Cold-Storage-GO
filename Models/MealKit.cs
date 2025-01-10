using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Cold_Storage_GO.Models
{
    public class MealKit
    {
        public Guid MealKitId { get; set; }
        public Guid DishId { get; set; }

        public string Name { get; set; }
        public int Price { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime ExpiryDate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; }

        public byte[]? ListingImage { get; set; }  // Stores the Image as a Blob (byte array)

        [NotMapped]
        public List<string>? Tags { get; set; }  // A list of tags like "Vegetarian", "Gluten-Free", "Halal"

        [Column("Tags")]
        public string? TagsSerialized
        {
            get => Tags != null ? JsonSerializer.Serialize(Tags) : null;
            set => Tags = value != null ? JsonSerializer.Deserialize<List<string>>(value) : null;
        }
    }
}
