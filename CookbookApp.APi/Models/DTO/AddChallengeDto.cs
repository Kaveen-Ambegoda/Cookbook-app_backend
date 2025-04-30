using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class AddChallengeDto
    {

        [Required]
        [StringLength(50)]
        public required string FullName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string ChallengeCategory { get; set; }

        [Required]
        [StringLength(50)]
        public required string ReasonForChoosing { get; set; }

        [Required]
        public required bool TermsAccepted { get; set; }

    }
}
