using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class DiscussionImage
    {
        [Key]
        public Guid ImageId { get; set; }

        [Required]
        public byte[] ImageData { get; set; } // Stores the image in binary format

        [Required]
        public Guid DiscussionId { get; set; }

        [ForeignKey("DiscussionId")]
        [JsonIgnore] // Prevent infinite recursion during serialization
        public Discussion? Discussion { get; set; }
    }
}
