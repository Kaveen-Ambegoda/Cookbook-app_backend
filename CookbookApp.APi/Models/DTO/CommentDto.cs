using System.Collections.Generic;
using System;

namespace CookbookApp.APi.Models.DTO
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid ForumId { get; set; }
        public int UserId { get; set; } // <-- CHANGE THIS FROM string to int
        public string Username { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }
        public List<ReplyDto> Replies { get; set; }
    }
}