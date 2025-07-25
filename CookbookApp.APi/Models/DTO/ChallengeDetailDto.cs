namespace CookbookApp.APi.Models.DTO
{
    public class ChallengeDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ChallengeCategory { get; set; }
        public string Description { get; set; }
        public TimelineDto Timeline { get; set; }
        public List<string> Requirements { get; set; }
    }
}
