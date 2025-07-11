using System;

namespace CookbookApp.APi.Models.Domain
{
    public class UserFavorite
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid ForumId { get; set; }

        // Navigation property
        public Forum Forum { get; set; }
    }
}