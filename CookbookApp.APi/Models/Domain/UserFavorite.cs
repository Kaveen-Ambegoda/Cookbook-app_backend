using CookbookAppBackend.Models;

namespace CookbookApp.APi.Models.Domain
{
    public class UserFavorite
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public Guid ForumId { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Forum Forum { get; set; }
    }
}