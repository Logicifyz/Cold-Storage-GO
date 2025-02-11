using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // Add this using

namespace Cold_Storage_GO.Models
{
    [Table("orders")] // Ensures the table is named "orders"
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string OrderType { get; set; }  // e.g. "Delivery", "Pickup"

        // Navigation property – list of order items (each referencing a MealKit)
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [Required]
        public Guid UserId { get; set; } // Taken from session

        [Required]
        public string DeliveryAddress { get; set; }

        [Required]
        public string OrderStatus { get; set; } // e.g. "Pending", "Shipped", etc.

        [Required]
        public DateTime OrderTime { get; set; } = DateTime.UtcNow;

        // ShipTime can be null if not yet shipped
        public DateTime? ShipTime { get; set; }

        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }
    }

    [Table("orderitems")] // Ensures the table is named "orderitems"
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [ForeignKey("OrderId")]
        [JsonIgnore] // Prevents the serializer from including the parent Order and breaking the cycle
        public Order Order { get; set; }

        [Required]
        public Guid MealKitId { get; set; }

        [ForeignKey("MealKitId")]
        public MealKit MealKit { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }
}
