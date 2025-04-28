namespace CookbookApp.APi.Models.Domain
{
    public class ChallengeParticipant
    {
        public Guid Id { get; set; }
        public string ChallengeId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Category { get; set; }
        public string Motivation { get; set; }
        public DateTime JoinDate { get; set; }
    }
}
