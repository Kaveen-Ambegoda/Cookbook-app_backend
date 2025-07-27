using CookbookApp.APi.Models;
using CookbookApp.APi.Models.Domain;
using System.ComponentModel.DataAnnotations;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int RecipeId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, MinLength(1)]
    public string Comment { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Recipe Recipe { get; set; }
    public User User { get; set; }
}
