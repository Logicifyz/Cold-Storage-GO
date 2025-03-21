﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Cold_Storage_GO.Models
{
    public class MealKit
    {
        public Guid MealKitId { get; set; }

        // Changed DishId to a list of DishId attributes
        [NotMapped]
        public List<Guid>? DishIds { get; set; }

        [Column("DishIds")]
        public string? DishIdsSerialized
        {
            get => DishIds != null ? JsonSerializer.Serialize(DishIds) : null;
            set => DishIds = value != null ? JsonSerializer.Deserialize<List<Guid>>(value) : null;
        }

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

        // Added Ingredients as a longtext (varchar)
        [Column(TypeName = "longtext")]
        public string? Ingredients { get; set; }
    }
}