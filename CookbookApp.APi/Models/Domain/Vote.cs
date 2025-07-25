using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class Vote
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserEmail { get; set; }
        
        [Required]
        public Guid SubmissionId { get; set; }
        
        [Required]
        public string ChallengeId { get; set; }
        
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public Submission Submission { get; set; }
    }
}
