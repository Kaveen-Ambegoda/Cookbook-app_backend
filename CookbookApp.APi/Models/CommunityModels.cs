using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CookbookAppBackend.Models
{
    // Enum for vote types
    public enum VoteType
    {
        Upvote = 1,
        Downvote = -1
    }

    // UserVote Entity
    public class UserVote
    {
        public int Id { get; set; }
        public int ForumId { get; set; }
        public int UserId { get; set; }
        public VoteType VoteType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("ForumId")]
        public virtual Forum Forum { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    // Forum Entity
    public class Forum
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Url { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int Views { get; set; } = 0;
        public int Upvotes { get; set; } = 0;
        public int Downvotes { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public int UserId { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<UserVote> UserVotes { get; set; } = new List<UserVote>();
    }

    // Comment Entity
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int ForumId { get; set; }
        public int UserId { get; set; }

        // Navigation Properties
        [ForeignKey("ForumId")]
        public virtual Forum Forum { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Reply> Replies { get; set; } = new List<Reply>();
    }

    // Reply Entity
    public class Reply
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int CommentId { get; set; }
        public int UserId { get; set; }

        // Navigation Properties
        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    // Favorite Entity
    public class Favorite
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int ForumId { get; set; }
        public int UserId { get; set; }

        // Navigation Properties
        [ForeignKey("ForumId")]
        public virtual Forum Forum { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    // DTOs for API responses
    public class ForumDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public int Comments { get; set; }
        public int Views { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserVote { get; set; } // "upvote", "downvote", or null
    }

    public class CommentDto
    {
        public string Id { get; set; } = string.Empty;
        public string ForumId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public List<ReplyDto> Replies { get; set; } = new List<ReplyDto>();
    }

    public class ReplyDto
    {
        public string Id { get; set; } = string.Empty;
        public string CommentId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    // Request models
    public class CreateForumRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Url { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }

    public class CreateCommentRequest
    {
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    public class CreateReplyRequest
    {
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }
}