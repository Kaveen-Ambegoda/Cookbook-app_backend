using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class Submission
    {
        public Guid Id { get; set; }
        
        [Required]
        public string FullName { get; set; }
        
        [Required]
        public string RecipeName { get; set; }
        
        [Required]
        public string Ingredients { get; set; } // JSON string of ingredients array
        
        [Required]
        public string RecipeDescription { get; set; }
        
        public string? RecipeImage { get; set; }
        
        [Required]
        public string ChallengeId { get; set; }
        
        [Required]
        public string ChallengeName { get; set; }
        
        [Required]
        public string ChallengeCategory { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public bool IsApproved { get; set; } = false;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Navigation properties
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        
        // Computed properties
        public int VotesCount => Votes?.Count ?? 0;
        public double AverageRating => Ratings?.Any() == true ? Ratings.Average(r => r.Stars) : 0;

        // Add these properties:
        [Required]
        public int UserId { get; set; } // Foreign key to User

        // If you want to store Email directly (not recommended, but possible):
        public string UserEmail { get; set; }
    }
    
}
