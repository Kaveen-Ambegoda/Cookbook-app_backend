using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class RecipeSubmissionDto
    {
        [Required]
        public int ChallengeId { get; set; }

        [Required]
        [StringLength(100)]
        public string RecipeName { get; set; }

        [Required]
        public string Ingredients { get; set; } // JSON array

        [Required]
        public string Description { get; set; }

        public IFormFile Image { get; set; }
    }
}