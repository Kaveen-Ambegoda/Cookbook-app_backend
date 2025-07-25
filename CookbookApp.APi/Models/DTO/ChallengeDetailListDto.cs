namespace CookbookApp.APi.Models.DTO
{
    public class ChallengeDetailListDto
    {
        public int Id { get; set; } // Add this line
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Img { get; set; }
        public string Date { get; set; }
        public string Sponsor { get; set; }
    }
}
