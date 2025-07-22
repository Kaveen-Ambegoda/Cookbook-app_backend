namespace CookbookApp.APi.Models.DTO.Admin.Recipe
{
    public class AdminUserDetailDto
    {
        // Core user
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }

        // Admin overlay
        public string Status { get; set; }
        public bool Reported { get; set; }
        public int ReportCount { get; set; }
        public string? ReportReason { get; set; }

        public string? SubscriptionType { get; set; }
        public DateTime? SubscriptionEnd { get; set; }

        public int? LiveVideos { get; set; }
        public int? Posts { get; set; }
        public int? Events { get; set; }
        public int? Followers { get; set; }

        public int Likes { get; set; }
        public int Comments { get; set; }
        public int VideosWatched { get; set; }
        public double EngagementScore { get; set; }

        public bool CanComment { get; set; }
        public bool CanLike { get; set; }
        public bool CanPost { get; set; }
        public bool CanMessage { get; set; }
        public bool CanLiveStream { get; set; }
    }
}
