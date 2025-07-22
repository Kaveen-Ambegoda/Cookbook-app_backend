using System.Collections.Generic;
using System.Threading.Tasks;
using CookbookApp.APi.Models;
using CookbookApp.APi.Models.DTO.Admin.Notification;
namespace CookbookApp.APi.Services
{
    public interface INotificationService
    {
        Task<PagedNotificationsResponse> GetPagedAsync(NotificationQuery query);
        Task<NotificationDto?> GetByIdAsync(Guid id);
        Task<NotificationDto?> UpdateStatusAsync(Guid id, NotificationStatus status);
        Task<NotificationDto?> MarkReadAsync(Guid id, bool isRead = true);
        Task<int> MarkAllReadAsync();
        Task<NotificationStatsDto> GetStatsAsync();
    }
}
