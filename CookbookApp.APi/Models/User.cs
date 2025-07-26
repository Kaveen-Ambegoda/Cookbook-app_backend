using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CookbookAppBackend.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "normal"; // 'normal', 'host', 'admin'

        [StringLength(20)]
        public string Status { get; set; } = "active"; // 'active', 'banned', 'restricted'

        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;

        public DateTime LastActive { get; set; } = DateTime.UtcNow;

        public int ReportCount { get; set; } = 0;

        [StringLength(500)]
        public string? ReportReason { get; set; }

        public bool Reported { get; set; } = false;

        [StringLength(500)]
        public string? Avatar { get; set; }

        [StringLength(20)]
        public string? SubscriptionType { get; set; } // 'monthly', 'annual', null

        public DateTime? SubscriptionEnd { get; set; }

        public int LiveVideos { get; set; } = 0;

        public int Posts { get; set; } = 0;

        public int Events { get; set; } = 0;

        public int Followers { get; set; } = 0;

        public int Likes { get; set; } = 0;

        public int Comments { get; set; } = 0;

        public int VideosWatched { get; set; } = 0;

        public int EngagementScore { get; set; } = 0;

        // Navigation property for restrictions
        public UserRestrictions? Restrictions { get; set; }

        public bool IsEmailConfirmed { get; set; }

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiryTime { get; set; }


        // JWT Related (keeping your existing properties)
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserRestrictions
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public bool Commenting { get; set; } = false;

        public bool Liking { get; set; } = false;

        public bool Posting { get; set; } = false;

        public bool Messaging { get; set; } = false;

        public bool LiveStreaming { get; set; } = false;

        // Navigation property
        public User User { get; set; } = null!;
    }
}