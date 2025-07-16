namespace CookbookApp.APi.Models.DTO
{
    public class SubmissionDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string RecipeName { get; set; }
        public List<string> Ingredients { get; set; } = new List<string>();
        public string RecipeDescription { get; set; }
        public string? RecipeImage { get; set; }
        public string ChallengeId { get; set; }
        public string ChallengeName { get; set; }
        public string ChallengeCategory { get; set; }
        public string UserEmail { get; set; }
        public string UserFullName { get; set; }
        public string CreatedAt { get; set; }
        public string Status { get; set; }
        public bool IsApproved { get; set; }
        public int VotesCount { get; set; }
        public double AverageRating { get; set; }
        public bool HasUserVoted { get; set; }
        public int? UserRating { get; set; }
    }
}
