using CookbookApp.APi.Models;
using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class FavoriteRecipe
    {
        [Key]
        public int FavoriteRecipeId { get; set; }

        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        public int UserId { get; set; }         
        public User? User { get; set; }        

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
