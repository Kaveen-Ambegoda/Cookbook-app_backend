using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO.Admin.Recipe
{
    public class RecipeDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; }

        [Range(1, 1440, ErrorMessage = "Cooking time must be between 1 and 1440 minutes")]
        public int CookingTime { get; set; }

        [Range(1, 50, ErrorMessage = "Portion must be between 1 and 50")]
        public int Portion { get; set; }

        [Required(ErrorMessage = "Ingredients are required")]
        public string Ingredients { get; set; }

        [Required(ErrorMessage = "Instructions are required")]
        public string Instructions { get; set; }

        [Range(0, 5000, ErrorMessage = "Calories must be between 0 and 5000")]
        public double Calories { get; set; }

        [Range(0, 500, ErrorMessage = "Protein must be between 0 and 500")]
        public double Protein { get; set; }

        [Range(0, 500, ErrorMessage = "Fat must be between 0 and 500")]
        public double Fat { get; set; }

        [Range(0, 500, ErrorMessage = "Carbs must be between 0 and 500")]
        public double Carbs { get; set; }

        public string Image { get; set; }

        [Required(ErrorMessage = "UserID is required")]
        public int UserID { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = "";

        public bool Visible { get; set; } = true;
    }

    public class ToggleVisibilityDto
    {
        public bool? Visible { get; set; }
    }
}