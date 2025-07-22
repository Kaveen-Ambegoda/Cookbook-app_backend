using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using CookbookApp.APi.Data;                               // CookbookDbContext
using CookbookApp.APi.Models;                             // Notification entity + enums + NotificationQuery
using CookbookApp.APi.Models.DTO.Admin.Notification;      // DTOs: NotificationDto, PagedNotificationsResponse, NotificationStatsDto

namespace CookbookApp.APi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly CookbookDbContext _db;

        public NotificationService(CookbookDbContext db)
        {
            _db = db;
        }
        private static string? BuildTargetUrl(Notification n)
        {
            if (!string.IsNullOrWhiteSpace(n.TargetUrl))
                return n.TargetUrl;

            return n.Type switch
            {
                NotificationType.RecipeReport => n.TargetId.HasValue ? $"/admin/recipes/{n.TargetId}" : "/admin/recipes",
                NotificationType.RecipeApproval => n.TargetId.HasValue ? $"/admin/recipes/{n.TargetId}" : "/admin/recipes",
                NotificationType.UserReport => n.TargetId.HasValue ? $"/admin/users/{n.TargetId}" : "/admin/users",
                // Malfunction or anything else has no specific target page.
                _ => null
            };
        }

        public async Task<PagedNotificationsResponse> GetPagedAsync(NotificationQuery query)
        {
            var q = _db.Notifications.AsQueryable();

            if (query.Type.HasValue)
                q = q.Where(n => n.Type == query.Type.Value);

            if (query.Status.HasValue)
                q = q.Where(n => n.Status == query.Status.Value);

            if (query.Priority.HasValue)
                q = q.Where(n => n.Priority == query.Priority.Value);

            if (query.IsRead.HasValue)
                q = q.Where(n => n.IsRead == query.IsRead.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(n =>
                    n.Title.Contains(s) ||
                    n.Description.Contains(s) ||
                    (n.ReporterName != null && n.ReporterName.Contains(s)) ||
                    (n.TargetName != null && n.TargetName.Contains(s))
                );
            }

            // Count before paging
            var total = await q.CountAsync();

            // Sort newest first
            q = q.OrderByDescending(n => n.CreatedUtc);

            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 200);

            var items = await q.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

            // Compute TargetUrl (not persisted; in-memory only)
            foreach (var n in items)
            {
                n.TargetUrl = BuildTargetUrl(n);
            }

            return new PagedNotificationsResponse
            {
                Items = items.Select(NotificationDto.FromEntity).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<NotificationDto?> GetByIdAsync(Guid id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n is null) return null;

            // Compute TargetUrl for single fetch
            n.TargetUrl = BuildTargetUrl(n);

            return NotificationDto.FromEntity(n);
        }

        public async Task<NotificationDto?> UpdateStatusAsync(Guid id, NotificationStatus status)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n is null) return null;

            n.Status = status;
            n.IsRead = true;

            // Ensure TargetUrl available (optional persist)
            if (string.IsNullOrWhiteSpace(n.TargetUrl))
                n.TargetUrl = BuildTargetUrl(n);

            await _db.SaveChangesAsync();
            return NotificationDto.FromEntity(n);
        }

        public async Task<NotificationDto?> MarkReadAsync(Guid id, bool isRead = true)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n is null) return null;

            n.IsRead = isRead;

            // Optional: generate TargetUrl if empty
            if (string.IsNullOrWhiteSpace(n.TargetUrl))
                n.TargetUrl = BuildTargetUrl(n);

            await _db.SaveChangesAsync();
            return NotificationDto.FromEntity(n);
        }

        public async Task<int> MarkAllReadAsync()
        {
            // Bulk set IsRead = true (no TargetUrl writes in bulk op)
            var updated = await _db.Notifications
                                   .Where(n => !n.IsRead)
                                   .ExecuteUpdateAsync(set => set.SetProperty(n => n.IsRead, true));
            return updated;
        }

        public async Task<NotificationStatsDto> GetStatsAsync()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var todayStartUtc = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);

            var q = _db.Notifications.AsNoTracking();

            var total = await q.CountAsync();
            var unread = await q.Where(n => !n.IsRead).CountAsync();
            var pending = await q.Where(n => n.Status == NotificationStatus.Pending).CountAsync();
            var highPriority = await q.Where(n => n.Priority == NotificationPriority.High || n.Priority == NotificationPriority.Urgent).CountAsync();
            var today = await q.Where(n => n.CreatedUtc >= todayStartUtc).CountAsync();

            var recipeApproval = await q.Where(n => n.Type == NotificationType.RecipeApproval).CountAsync();
            var userReport = await q.Where(n => n.Type == NotificationType.UserReport).CountAsync();
            var recipeReport = await q.Where(n => n.Type == NotificationType.RecipeReport).CountAsync();
            var malfunction = await q.Where(n => n.Type == NotificationType.Malfunction).CountAsync();

            return new NotificationStatsDto
            {
                Total = total,
                Unread = unread,
                Pending = pending,
                HighPriority = highPriority,
                Today = today,
                RecipeApproval = recipeApproval,
                UserReport = userReport,
                RecipeReport = recipeReport,
                Malfunction = malfunction
            };
        }
    }
}
