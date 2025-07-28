namespace CookbookApp.APi.Models.DTO
{
    public class UserProfileDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? PersonalLinks { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Status { get; set; }
    }

}
