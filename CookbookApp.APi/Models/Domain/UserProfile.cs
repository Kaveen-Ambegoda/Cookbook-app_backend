using CookbookAppBackend.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [Range(15, 100, ErrorMessage = "Age must be between 15 and 100")]
        public int Age { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; } // "male" or "female"

        [Required]
        [Range(30, 300, ErrorMessage = "Weight must be between 30 and 300 kg")]
        public decimal Weight { get; set; }

        [Required]
        [Range(100, 250, ErrorMessage = "Height must be between 100 and 250 cm")]
        public decimal Height { get; set; }

        [Required]
        [StringLength(20)]
        public string ActivityLevel { get; set; } // sedentary, light, moderate, active, very_active

        [Range(5, 50, ErrorMessage = "Body fat percentage must be between 5 and 50")]
        public decimal? BodyFatPercentage { get; set; }

        [Required]
        [StringLength(20)]
        public string Goal { get; set; } // maintain, lose, gain

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

