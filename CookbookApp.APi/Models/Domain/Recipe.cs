using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CookbookApp.API.Models.Domain
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; } // Recipe ID

        public string Title { get; set; }

        public string Category { get; set; }

        public int CookingTime { get; set; } // in minutes

        public int Portion { get; set; } 

        public string Ingredients { get; set; }

        public string Instructions { get; set; }

        public string Calories { get; set; }

        public string Protein { get; set; }

        public string Fat { get; set; }

        public string Carbs { get; set; } 

        public string ImageUrl { get; set; }
    }
}
