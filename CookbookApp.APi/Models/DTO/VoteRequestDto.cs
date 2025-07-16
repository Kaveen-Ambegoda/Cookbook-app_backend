using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class VoteRequestDto
    {
        [Required]
        public string UserEmail { get; set; }
        
        [Required]
        public Guid SubmissionId { get; set; }
        
        [Required]
        public string ChallengeId { get; set; }
    }
}
