namespace CookbookApp.APi.Models.DTO
{
    public class CreateChallengeRequest
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Date { get; set; }
        public string Sponsor { get; set; }
        public IFormFile Image { get; set; }  // For Cloudinary upload
        public string Description { get; set; }
        public List<string> Requirements { get; set; }
        public string TimelineRegistration { get; set; }
        public string TimelineJudging { get; set; }
        public string TimelineWinnersAnnounced { get; set; }
    }
}
