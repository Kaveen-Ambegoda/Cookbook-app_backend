namespace CookbookApp.APi.Models
{
    public enum NotificationType
    {
        UserReport,
        RecipeApproval,
        Malfunction,
        RecipeReport
    }

    public enum NotificationStatus
    {
        Pending,
        Approved,
        Rejected,
        Resolved,
        Dismissed
    }

    public enum NotificationPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum NotificationTargetType
    {
        User,
        Recipe
    }
}
