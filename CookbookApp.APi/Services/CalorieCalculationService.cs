using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;

namespace CookbookApp.APi.Services
{
    public interface ICalorieCalculationService
    {
        CalorieCalculation CalculateCalories(UserProfile userProfile);
        string GetBMICategory(decimal bmi);
        (decimal min, decimal max) GetIdealWeightRange(decimal heightCm);
        MacronutrientBreakdownDto CalculateMacros(decimal totalCalories, string goal);
    }

    public class CalorieCalculationService : ICalorieCalculationService
    {
        private readonly Dictionary<string, decimal> _activityMultipliers = new()
        {
            { "sedentary", 1.2m },      // Little to no exercise
            { "light", 1.375m },        // Light exercise 1-3 days/week
            { "moderate", 1.55m },      // Moderate exercise 3-5 days/week
            { "active", 1.725m },       // Heavy exercise 6-7 days/week
            { "very_active", 1.9m }     // Very heavy exercise, physical job
        };

        public CalorieCalculation CalculateCalories(UserProfile userProfile)
        {
            // Calculate BMR using appropriate formula
            decimal bmr = userProfile.BodyFatPercentage.HasValue
                ? CalculateBMRWithBodyFat(userProfile)
                : CalculateBMRStandard(userProfile);

            // Calculate TDEE (Total Daily Energy Expenditure)
            decimal maintenance = CalculateTDEE(bmr, userProfile.ActivityLevel);

            // Calculate weight loss and gain calories
            decimal weightLoss = Math.Max(maintenance - 500, bmr); // Don't go below BMR
            decimal weightGain = maintenance + 500;

            // Calculate BMI and ideal weight range
            decimal bmi = CalculateBMI(userProfile.Weight, userProfile.Height);
            var idealWeight = GetIdealWeightRange(userProfile.Height);

            // Calculate macronutrients based on maintenance calories
            var macros = CalculateMacros(maintenance, userProfile.Goal);

            return new CalorieCalculation
            {
                UserId = userProfile.UserId,
                UserProfileId = userProfile.Id,
                BMR = Math.Round(bmr, 0),
                MaintenanceCalories = Math.Round(maintenance, 0),
                WeightLossCalories = Math.Round(weightLoss, 0),
                WeightGainCalories = Math.Round(weightGain, 0),
                BMI = Math.Round(bmi, 1),
                IdealWeightMin = Math.Round(idealWeight.min, 1),
                IdealWeightMax = Math.Round(idealWeight.max, 1),
                ProteinGrams = Math.Round(macros.Protein.Grams, 0),
                CarbsGrams = Math.Round(macros.Carbs.Grams, 0),
                FatGrams = Math.Round(macros.Fat.Grams, 0),
                ProteinCalories = Math.Round(macros.Protein.Calories, 0),
                CarbsCalories = Math.Round(macros.Carbs.Calories, 0),
                FatCalories = Math.Round(macros.Fat.Calories, 0)
            };
        }

        private decimal CalculateBMRStandard(UserProfile userProfile)
        {
            // Mifflin-St Jeor Equation
            // Men: BMR = 10 × weight(kg) + 6.25 × height(cm) - 5 × age(years) + 5
            // Women: BMR = 10 × weight(kg) + 6.25 × height(cm) - 5 × age(years) - 161

            decimal baseBMR = 10 * userProfile.Weight + 6.25m * userProfile.Height - 5 * userProfile.Age;

            if (userProfile.Gender.ToLower() == "male")
            {
                return baseBMR + 5;
            }
            else
            {
                return baseBMR - 161;
            }
        }

        private decimal CalculateBMRWithBodyFat(UserProfile userProfile)
        {
            // Katch-McArdle Formula: BMR = 370 + (21.6 × lean body mass in kg)
            if (!userProfile.BodyFatPercentage.HasValue)
                return CalculateBMRStandard(userProfile);

            decimal leanBodyMass = userProfile.Weight * (1 - userProfile.BodyFatPercentage.Value / 100);
            return 370 + (21.6m * leanBodyMass);
        }

        private decimal CalculateTDEE(decimal bmr, string activityLevel)
        {
            if (_activityMultipliers.TryGetValue(activityLevel.ToLower(), out decimal multiplier))
            {
                return bmr * multiplier;
            }

            // Default to sedentary if activity level not found
            return bmr * _activityMultipliers["sedentary"];
        }

        private decimal CalculateBMI(decimal weightKg, decimal heightCm)
        {
            decimal heightM = heightCm / 100;
            return weightKg / (heightM * heightM);
        }

        public string GetBMICategory(decimal bmi)
        {
            return bmi switch
            {
                < 18.5m => "Underweight",
                < 25m => "Normal weight",
                < 30m => "Overweight",
                _ => "Obese"
            };
        }

        public (decimal min, decimal max) GetIdealWeightRange(decimal heightCm)
        {
            decimal heightM = heightCm / 100;
            decimal minWeight = 18.5m * heightM * heightM;
            decimal maxWeight = 24.9m * heightM * heightM;

            return (minWeight, maxWeight);
        }

        public MacronutrientBreakdownDto CalculateMacros(decimal totalCalories, string goal)
        {
            // Macronutrient percentages based on goal
            var (proteinPercent, fatPercent, carbPercent) = goal.ToLower() switch
            {
                "lose" => (0.30m, 0.25m, 0.45m),      // Higher protein for muscle preservation
                "gain" => (0.25m, 0.25m, 0.50m),      // Higher carbs for energy
                _ => (0.25m, 0.30m, 0.45m)             // Maintenance - balanced
            };

            var proteinCalories = totalCalories * proteinPercent;
            var fatCalories = totalCalories * fatPercent;
            var carbCalories = totalCalories * carbPercent;

            return new MacronutrientBreakdownDto
            {
                Protein = new MacronutrientDto
                {
                    Calories = proteinCalories,
                    Grams = proteinCalories / 4, // 4 calories per gram
                    Percentage = (int)(proteinPercent * 100)
                },
                Fat = new MacronutrientDto
                {
                    Calories = fatCalories,
                    Grams = fatCalories / 9, // 9 calories per gram
                    Percentage = (int)(fatPercent * 100)
                },
                Carbs = new MacronutrientDto
                {
                    Calories = carbCalories,
                    Grams = carbCalories / 4, // 4 calories per gram
                    Percentage = (int)(carbPercent * 100)
                }
            };
        }
    }

}
