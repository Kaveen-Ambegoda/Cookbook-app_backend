namespace CookbookApp.APi.Models.DTO
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public string? Username { get; set; }
        public string? RecipeName { get; set; }
        public string? RecipeImage { get; set; }
    }

}
