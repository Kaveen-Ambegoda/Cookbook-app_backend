using System;

namespace CookbookApp.APi.Models.DTO
{
    public class ReplyDto
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }
    }
}