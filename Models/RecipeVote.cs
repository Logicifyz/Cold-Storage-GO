using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cold_Storage_GO.Models
{
    public class RecipeVote
    {
        [Key]
        public Guid VoteId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RecipeId { get; set; }  

        [Required]
        [Range(-1, 1)] 
        public int VoteType { get; set; }

        [JsonIgnore]  
        public Recipe Recipe { get; set; }
    }
}
