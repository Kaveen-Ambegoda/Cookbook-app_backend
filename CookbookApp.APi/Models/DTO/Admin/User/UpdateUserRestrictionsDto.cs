namespace CookbookApp.APi.Models.DTO.Admin.User
{
    public class UpdateUserRestrictionsDto
    {
        public bool CanComment { get; set; }
        public bool CanLike { get; set; }
        public bool CanPost { get; set; }
        public bool CanMessage { get; set; }
        public bool CanLiveStream { get; set; }
    }
}
