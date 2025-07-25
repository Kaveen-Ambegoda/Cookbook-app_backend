using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserEmail { get; set; }
        
        [Required]
        public Guid SubmissionId { get; set; }
        
        [Required]
        public string ChallengeId { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Stars { get; set; }
        
        public DateTime RatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public Submission Submission { get; set; }
    }
}
