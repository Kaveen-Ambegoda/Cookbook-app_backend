using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class CreateSubmissionRequestDto
    {
        [Required]
        public string DisplayName { get; set; }
        
        [Required]
        public string RecipeName { get; set; }
        
        [Required]
        public List<string> Ingredients { get; set; } = new List<string>();
        
        [Required]
        public string RecipeDescription { get; set; }
        
        public IFormFile? RecipeImage { get; set; }
        
        [Required]
        public string ChallengeId { get; set; }
        
        [Required]
        public string ChallengeName { get; set; }
        
        [Required]
        public string ChallengeCategory { get; set; }
        
        [Required]
        public string UserEmail { get; set; }
        
        [Required]
        public string UserFullName { get; set; }
    }
}