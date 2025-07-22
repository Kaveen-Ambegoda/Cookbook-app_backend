// Location: Models/DTO/UpdateRecipeDto.cs

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CookbookApp.APi.Models.DTO
{
    public class UpdateRecipeDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Ingredients { get; set; } = string.Empty;

        [Required]
        public string Instructions { get; set; } = string.Empty;

        public int CookingTime { get; set; }
        public int Portion { get; set; }
        public int Calories { get; set; }
        public int Protein { get; set; }
        public int Fat { get; set; }
        public int Carbs { get; set; }

        // --- THIS IS THE CRITICAL FIX ---
        // This property must be IFormFile to handle the file upload.
        public IFormFile? Image { get; set; }
    }
}