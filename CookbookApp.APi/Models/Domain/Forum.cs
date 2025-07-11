using System;
using System.Collections.Generic;

namespace CookbookApp.APi.Models.Domain
{
    public class Forum
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CommentsCount { get; set; }
        public int ViewsCount { get; set; }
        public int UpvotesCount { get; set; }
        public int DownvotesCount { get; set; }
        public string AuthorId { get; set; }
        public string Category { get; set; }

        // Navigation properties
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
    }
}