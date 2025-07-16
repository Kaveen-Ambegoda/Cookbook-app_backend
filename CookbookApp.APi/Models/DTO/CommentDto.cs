using System;
using System.Collections.Generic;

namespace CookbookApp.APi.Models.DTO
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid ForumId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }
        public List<ReplyDto> Replies { get; set; } = new List<ReplyDto>();
    }
}