namespace CookbookApp.APi.Models.DTO
{
    public class SubmissionChallengeDto
    {
        public Guid SubmissionId { get; set; }
        public string ChallengeId { get; set; }
        public string FullName { get; set; }
        public string RecipeName { get; set; }
        public List<string> Ingredients { get; set; }
        public string RecipeDescription { get; set; }
        public string RecipeImage { get; set; }
        public string ChallengeCategory { get; set; }
    }
}
