namespace CookbookApp.APi.Models.DTO.Admin.User
{
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public bool Reported { get; set; }
        public int ReportCount { get; set; }
        public double EngagementScore { get; set; }
    }
}
