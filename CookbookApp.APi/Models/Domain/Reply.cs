using CookbookAppBackend.Models;

namespace CookbookApp.APi.Models.Domain
{
    public class Reply
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Comment Comment { get; set; }
        public User User { get; set; }
    }
}