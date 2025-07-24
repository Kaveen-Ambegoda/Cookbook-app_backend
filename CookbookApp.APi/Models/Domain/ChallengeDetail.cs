namespace CookbookApp.APi.Models.Domain
{
    public class ChallengeDetail
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Date { get; set; }
        public string Sponsor { get; set; }
        public string ImgUrl { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }  // Store as JSON or comma-separated
        public string TimelineRegistration { get; set; }
        public string TimelineJudging { get; set; }
        public string TimelineWinnersAnnounced { get; set; }
    }
}
