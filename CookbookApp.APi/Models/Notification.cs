using System;
using CookbookApp.APi.Models;
using System.Text.Json;
namespace CookbookApp.APi.Models
{
    public class Notification
    {
        public Guid Id { get; set; }

        public NotificationType Type { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;

        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;

        public Guid? ReporterId { get; set; }
        public string? ReporterName { get; set; }

        public Guid? TargetId { get; set; }
        public string? TargetName { get; set; }
        public NotificationTargetType? TargetType { get; set; }
        public string? TargetUrl { get; set; }

        public string? Category { get; set; } // e.g., cuisine

        /// <summary>
        /// Arbitrary type-specific payload serialized as JSON.
        /// </summary>
        public string? DetailsJson { get; set; }

        public bool IsRead { get; set; } = false;

        // Helper: set/get details using a typed object or anonymous
        public T? GetDetails<T>()
        {
            if (string.IsNullOrWhiteSpace(DetailsJson)) return default;
            return JsonSerializer.Deserialize<T>(DetailsJson);
        }

        public void SetDetails<T>(T value)
        {
            DetailsJson = value is null ? null : JsonSerializer.Serialize(value);
        }
    }
}
