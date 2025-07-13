using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class SubmitForum
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChallengeId { get; set; }

        [Required]
        [StringLength(100)]
        public required string RecipeName { get; set; }

        [Required]
        public required string Ingredients { get; set; }

        [Required]
        public required string Description { get; set; }

        public string? ImageUrl { get; set; }
    }
}
