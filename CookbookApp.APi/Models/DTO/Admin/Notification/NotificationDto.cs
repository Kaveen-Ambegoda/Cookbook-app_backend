using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using CookbookApp.APi.Models;
using EntityNotification = CookbookApp.APi.Models.Notification;

namespace CookbookApp.APi.Models.DTO.Admin.Notification
{
    public class NotificationDto
    {
        public string Id { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Timestamp { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string Priority { get; set; } = default!;
        public string? ReporterId { get; set; }
        public string? ReporterName { get; set; }
        public string? TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? TargetType { get; set; }
        public string? TargetUrl { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object>? Details { get; set; }
        public bool IsRead { get; set; }

        public static NotificationDto FromEntity(EntityNotification n)
        {
            Dictionary<string, object>? details = null;
            if (!string.IsNullOrWhiteSpace(n.DetailsJson))
            {
                try
                {
                    // Use JsonNode -> Dictionary
                    var node = JsonNode.Parse(n.DetailsJson);
                    if (node is JsonObject obj)
                    {
                        details = obj.ToDictionary(
                            kvp => kvp.Key,
                            kvp =>
                                kvp.Value?.GetValueKind() switch
                                {
                                    JsonValueKind.Number => (object?)(kvp.Value?.GetValue<double>() ?? 0),
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    JsonValueKind.String => (object?)(kvp.Value?.GetValue<string>() ?? string.Empty),
                                    _ => kvp.Value?.ToJsonString() ?? string.Empty
                                } ?? string.Empty
                        );
                    }
                }
                catch { /* swallow parse errors */ }
            }

            return new NotificationDto
            {
                Id = n.Id.ToString(),
                Type = n.Type.ToString().ToLowerInvariant(), // front-end expects kebabish lower?
                Title = n.Title,
                Description = n.Description,
                Timestamp = n.CreatedUtc.ToUniversalTime().ToString("o"),
                Status = n.Status.ToString().ToLowerInvariant(),
                Priority = n.Priority.ToString().ToLowerInvariant(),
                ReporterId = n.ReporterId?.ToString(),
                ReporterName = n.ReporterName,
                TargetId = n.TargetId?.ToString(),
                TargetName = n.TargetName,
                TargetType = n.TargetType?.ToString().ToLowerInvariant(),
                TargetUrl = n.TargetUrl,
                Category = n.Category,
                Details = details,
                IsRead = n.IsRead
            };
        }
    }
}
