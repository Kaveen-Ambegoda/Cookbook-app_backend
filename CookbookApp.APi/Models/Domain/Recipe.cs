using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CookbookAppBackend.Models;
using CookbookApp.APi.Models.Domain;
namespace CookbookApp.APi.Models.Domain
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
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }
        public string Image { get; set; }

        // This links each recipe to a user (owner)
        public int UserID { get; set; }
        

        public  ICollection<Review> Reviews { get; set; } = new List<Review>(); // navigation property
        [ForeignKey("UserID")]
        public User User { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = "";

        public bool Visible { get; set; } = true;
    }
}

    

