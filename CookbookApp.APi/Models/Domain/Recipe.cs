namespace CookbookApp.APi.Models
{
    public class Recipe
    {
        public int Id { get; set; } // Recipe ID
        public string Name { get; set; } 
        public string ImageUrl { get; set; } 
        public int CookingTime { get; set; } // in minutes
        public int Servings { get; set; } // number of servings
    }
}
