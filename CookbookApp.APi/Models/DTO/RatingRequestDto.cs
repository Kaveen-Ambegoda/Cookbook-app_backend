using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class RatingRequestDto
    {
        [Required]
        public string UserEmail { get; set; }
        
        [Required]
        public Guid SubmissionId { get; set; }
        
        [Required]
        public string ChallengeId { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Stars { get; set; }
    }
}
