using CookbookAppBackend.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class CalorieCalculation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public int UserProfileId { get; set; }

        [ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; }

        [Required]
        public decimal BMR { get; set; } // Basal Metabolic Rate

        [Required]
        public decimal MaintenanceCalories { get; set; } // TDEE

        [Required]
        public decimal WeightLossCalories { get; set; }

        [Required]
        public decimal WeightGainCalories { get; set; }

        [Required]
        public decimal BMI { get; set; }

        [Required]
        public decimal IdealWeightMin { get; set; }

        [Required]
        public decimal IdealWeightMax { get; set; }

        // Macronutrient breakdown for maintenance calories
        public decimal ProteinGrams { get; set; }
        public decimal CarbsGrams { get; set; }
        public decimal FatGrams { get; set; }

        public decimal ProteinCalories { get; set; }
        public decimal CarbsCalories { get; set; }
        public decimal FatCalories { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}

