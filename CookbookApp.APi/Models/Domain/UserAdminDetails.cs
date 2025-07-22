using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    [Table("UserAdminDetails")]
    public class UserAdminDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public Userpartial User { get; set; } = null!;

        public string Status { get; set; } = "active"; // active|restricted|banned
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

        public bool CanComment { get; set; } = true;
        public bool CanLike { get; set; } = true;
        public bool CanPost { get; set; } = true;
        public bool CanMessage { get; set; } = true;
        public bool CanLiveStream { get; set; } = true;
    }
}