namespace CookbookApp.APi.Models.DTO.Admin.Recipe
{
    public class GetRecipeDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
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
