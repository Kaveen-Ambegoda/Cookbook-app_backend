using System;
using System.Collections.Generic;

namespace CookbookApp.APi.Models.Domain
{
    public class Comment
    {
        public Guid Id { get; set; }
        public Guid ForumId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Forum Forum { get; set; }
        public ICollection<Reply> Replies { get; set; } = new List<Reply>();
    }
}