using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.DTO
{
    public class UpdateProfileDto
    {
        [MaxLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
        public string? Bio { get; set; }

        [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        public string? Location { get; set; }

        [MaxLength(2000, ErrorMessage = "Personal links cannot exceed 2000 characters")]
        public string? PersonalLinks { get; set; }

        [Url(ErrorMessage = "Profile picture URL must be a valid URL")]
        public string? ProfilePictureUrl { get; set; }
    }

}
