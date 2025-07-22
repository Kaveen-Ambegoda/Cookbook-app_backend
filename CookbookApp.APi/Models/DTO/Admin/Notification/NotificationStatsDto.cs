namespace CookbookApp.APi.Models.DTO.Admin.Notification
{
    public class NotificationStatsDto
    {
        public int Total { get; set; }
        public int Unread { get; set; }
        public int Pending { get; set; }
        public int HighPriority { get; set; } // includes urgent
        public int Today { get; set; }
        public int RecipeApproval { get; set; }
        public int UserReport { get; set; }
        public int RecipeReport { get; set; }
        public int Malfunction { get; set; }
    }
}
