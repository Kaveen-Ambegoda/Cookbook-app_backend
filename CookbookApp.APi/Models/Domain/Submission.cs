using CookbookApp.APi.Models.Domain;
using CookbookAppBackend.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Submission
{
    public Guid Id { get; set; }

    public int RecipeId { get; set; }
    public string FullName { get; set; }
    public string RecipeName { get; set; }
    public string RecipeDescription { get; set; }
    public string Ingredients { get; set; }
    public string RecipeImage { get; set; }
    public string ChallengeName { get; set; }
    public string ChallengeCategory { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; }
    public bool IsApproved { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public int Votes { get; set; }
    public double Rating { get; set; }
    public int TotalRatings { get; set; }
    public string ChallengeId { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
