using System;

namespace CookbookApp.APi.Models.DTO
{
    public class ForumDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public string Url { get; set; }
        public string Timestamp { get; set; }
        public int Comments { get; set; }
        public int Views { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public bool IsFavorite { get; set; }
    }
}