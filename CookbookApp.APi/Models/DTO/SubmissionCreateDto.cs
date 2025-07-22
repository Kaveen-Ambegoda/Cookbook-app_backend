using Microsoft.AspNetCore.Http;

namespace CookbookApp.APi.Models.DTO
{
    public class SubmissionCreateDto
    {
        public string RecipeName { get; set; }
        public string RecipeDescription { get; set; }
        public string ChallengeId { get; set; }
        public string ChallengeName { get; set; }
        public string ChallengeCategory { get; set; }
        public string UserFullName { get; set; }
        public List<string> Ingredients { get; set; }
        public IFormFile? RecipeImage { get; set; }
    }
}
