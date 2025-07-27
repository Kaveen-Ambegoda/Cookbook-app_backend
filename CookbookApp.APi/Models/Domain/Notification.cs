using System.ComponentModel.DataAnnotations.Schema;

namespace CookbookApp.APi.Models.Domain
{
    // Models/Domain/Notification.cs
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int? RecipeId { get; set; }
        public Recipe? Recipe { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
    }



}
