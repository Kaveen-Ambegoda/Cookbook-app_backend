namespace CookbookApp.APi.Models.DTO
{
    public class RecipeDto
    {
        public int Id { get; set; } // Recipe ID
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int CookingTime { get; set; } // in minutes
        public int Servings { get; set; } // number of servings
    }
}
