using System;

namespace CookbookApp.APi.Models.Domain
{
    public class Reply
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public Comment Comment { get; set; }
    }
}