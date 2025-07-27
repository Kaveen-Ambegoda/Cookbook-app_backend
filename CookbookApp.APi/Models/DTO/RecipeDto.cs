namespace CookbookApp.APi.Models.DTO
{
    public class RecipeDto
    {
        public int Id { get; set; } // Recipe ID
        public string Title { get; set; }
        public string? MealType { get; set; }
        public string? Cuisine { get; set; }
        public string? Diet { get; set; }
        public string? Occasion { get; set; }
        public string? SkillLevel { get; set; }
        public int CookingTime { get; set; }
        public int Portion { get; set; }
        public string Ingredients { get; set; }
        public string Instructions { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }
        public string Image { get; set; }
    }
}
