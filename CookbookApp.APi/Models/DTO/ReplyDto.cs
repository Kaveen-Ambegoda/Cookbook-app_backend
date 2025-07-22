using System;

namespace CookbookApp.APi.Models.DTO
{
    public class ReplyDto
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public int UserId { get; set; } // <-- CHANGE THIS FROM string to int
        public string Username { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }
    }
}