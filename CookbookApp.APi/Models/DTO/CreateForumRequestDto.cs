using Microsoft.AspNetCore.Http;

namespace CookbookApp.APi.Models.DTO
{
    public class CreateForumRequestDto
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public IFormFile Image { get; set; }
    }
}