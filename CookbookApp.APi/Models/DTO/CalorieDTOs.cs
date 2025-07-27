using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class UserProfileRequestDto
    {
        [Required]
        [Range(15, 100, ErrorMessage = "Age must be between 15 and 100")]
        public int Age { get; set; }

        [Required]
        [RegularExpression("^(male|female)$", ErrorMessage = "Gender must be 'male' or 'female'")]
        public string Gender { get; set; }

        [Required]
        [Range(30, 300, ErrorMessage = "Weight must be between 30 and 300 kg")]
        public decimal Weight { get; set; }

        [Required]
        [Range(100, 250, ErrorMessage = "Height must be between 100 and 250 cm")]
        public decimal Height { get; set; }

        [Required]
        [RegularExpression("^(sedentary|light|moderate|active|very_active)$",
            ErrorMessage = "Activity level must be one of: sedentary, light, moderate, active, very_active")]
        public string ActivityLevel { get; set; }

        [Range(5, 50, ErrorMessage = "Body fat percentage must be between 5 and 50")]
        public decimal? BodyFatPercentage { get; set; }

        [Required]
        [RegularExpression("^(maintain|lose|gain)$",
            ErrorMessage = "Goal must be one of: maintain, lose, gain")]
        public string Goal { get; set; }
    }

    // Response DTO for user profile
    public class UserProfileResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public decimal Weight { get; set; }
        public decimal Height { get; set; }
        public string ActivityLevel { get; set; }
        public decimal? BodyFatPercentage { get; set; }
        public string Goal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Response DTO for calorie calculation results
    public class CalorieCalculationResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UserProfileResponseDto UserProfile { get; set; }

        // Core calorie calculations
        public decimal BMR { get; set; }
        public decimal MaintenanceCalories { get; set; }
        public decimal WeightLossCalories { get; set; }
        public decimal WeightGainCalories { get; set; }

        // Health metrics
        public decimal BMI { get; set; }
        public string BMICategory { get; set; }
        public decimal IdealWeightMin { get; set; }
        public decimal IdealWeightMax { get; set; }

        // Macronutrient breakdown
        public MacronutrientBreakdownDto Macros { get; set; }

        public DateTime CalculatedAt { get; set; }
    }

    // Macronutrient breakdown DTO
    public class MacronutrientBreakdownDto
    {
        public MacronutrientDto Protein { get; set; }
        public MacronutrientDto Carbs { get; set; }
        public MacronutrientDto Fat { get; set; }
    }

    public class MacronutrientDto
    {
        public decimal Grams { get; set; }
        public decimal Calories { get; set; }
        public int Percentage { get; set; }
    }

    // DTO for calculation history
    public class CalorieHistoryDto
    {
        public int Id { get; set; }
        public decimal BMR { get; set; }
        public decimal MaintenanceCalories { get; set; }
        public decimal WeightLossCalories { get; set; }
        public decimal WeightGainCalories { get; set; }
        public decimal BMI { get; set; }
        public decimal Weight { get; set; }
        public string Goal { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    // Response wrapper for API responses
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }


}

