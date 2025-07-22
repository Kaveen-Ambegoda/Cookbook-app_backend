
namespace CookbookApp.APi.Models
{
    public class NotificationQuery
    {
        public NotificationType? Type { get; set; }
        public NotificationStatus? Status { get; set; }
        public NotificationPriority? Priority { get; set; }
        public bool? IsRead { get; set; }
        public string? Search { get; set; }

        public int Page { get; set; } = 1;      // 1-based
        public int PageSize { get; set; } = 20;  // default
    }
}
