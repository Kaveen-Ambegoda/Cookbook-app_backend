// Models/DTO/UpdateForumRequestDto.cs

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class UpdateForumRequestDto
    {
        [Required]
        [StringLength(100, MinimumLength = 5)]
        public string Title { get; set; }

        public string? Url { get; set; }

        [Required]
        public string Category { get; set; }

        // The image is optional when updating
        public IFormFile? Image { get; set; }
    }
}