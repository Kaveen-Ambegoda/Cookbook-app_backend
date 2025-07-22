namespace CookbookApp.APi.Models.Domain
{
    public class Userpartial 
    {
        // Add navigation for admin details
        public UserAdminDetails? AdminDetails { get; set; }

        // Optional: If you want back reference to Recipes
        public ICollection<Recipe>? Recipes { get; set; }
    }
}
