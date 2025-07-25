using System.ComponentModel.DataAnnotations;
using System;

namespace CookbookApp.APi.Models.DTO.Admin.User
{
    public class UserDto
    {
            public int Id { get; set; }
            public required string Name { get; set; }
            public required string Email { get; set; }
            public string Role { get; set; } = "normal";
            public string Status { get; set; } = "active";
            public string RegisteredDate { get; set; } = string.Empty;
            public string LastActive { get; set; } = string.Empty;
            public int ReportCount { get; set; }
            public string? ReportReason { get; set; }
            public bool Reported { get; set; }
            public string? Avatar { get; set; }
            public string? SubscriptionType { get; set; }
            public string? SubscriptionEnd { get; set; }
            public int? LiveVideos { get; set; }
            public int? Posts { get; set; }
            public int? Events { get; set; }
            public int? Followers { get; set; }
            public int Likes { get; set; }
            public int Comments { get; set; }
            public int VideosWatched { get; set; }
            public int EngagementScore { get; set; }
            public UserRestrictionsDto? Restrictions { get; set; }
        }

        public class UserRestrictionsDto
        {
            public bool Commenting { get; set; }
            public bool Liking { get; set; }
            public bool Posting { get; set; }
            public bool Messaging { get; set; }
            public bool LiveStreaming { get; set; }
        }

        public class CreateUserDto
        {
            [Required]
            [StringLength(100)]
            public required string Name { get; set; }

            [Required]
            [EmailAddress]
            public required string Email { get; set; }

            [Required]
            [MinLength(6)]
            public required string Password { get; set; }

            public string Role { get; set; } = "normal";
        }

        public class UpdateUserDto
        {
            [StringLength(100)]
            public string? Name { get; set; }

            [EmailAddress]
            public string? Email { get; set; }

            public string? Role { get; set; }
            public string? Status { get; set; }
            public string? Avatar { get; set; }
            public string? SubscriptionType { get; set; }
            public DateTime? SubscriptionEnd { get; set; }
            public int? LiveVideos { get; set; }
            public int? Posts { get; set; }
            public int? Events { get; set; }
            public int? Followers { get; set; }
            public int? Likes { get; set; }
            public int? Comments { get; set; }
            public int? VideosWatched { get; set; }
            public int? EngagementScore { get; set; }
        }

        public class UpdateUserStatusDto
        {
            [Required]
            public required string Status { get; set; }
        }

        public class UpdateUserRoleDto
        {
            [Required]
            public required string Role { get; set; }
        }

        public class UpdateUserRestrictionsDto
        {
            public bool Commenting { get; set; }
            public bool Liking { get; set; }
            public bool Posting { get; set; }
            public bool Messaging { get; set; }
            public bool LiveStreaming { get; set; }
        }
    }
