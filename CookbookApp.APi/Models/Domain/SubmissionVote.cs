using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class SubmissionVote
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid SubmissionId { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string UserEmail { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public Submission Submission { get; set; }
    }
}
