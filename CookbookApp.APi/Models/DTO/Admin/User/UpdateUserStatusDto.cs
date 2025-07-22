namespace CookbookApp.APi.Models.DTO.Admin.User
{
    public class UpdateUserStatusDto
    {
        public string Status { get; set; } = "active"; // active|restricted|banned
    }
}
