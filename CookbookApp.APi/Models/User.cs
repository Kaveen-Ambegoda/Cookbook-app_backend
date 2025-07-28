using System;
using System.ComponentModel.DataAnnotations;

namespace CookbookAppBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public required string Username { get; set; }
        [Required]
        public required string Email { get; set; }
        [Required]

        public bool IsEmailConfirmed { get; set; }

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiryTime { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }  // 'Admin', 'User'

        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? PersonalLinks { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Status { get; set; } = "active";

        public DateTime? LastLoginAt { get; set; }

        public DateTime? EmailVerifiedAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
