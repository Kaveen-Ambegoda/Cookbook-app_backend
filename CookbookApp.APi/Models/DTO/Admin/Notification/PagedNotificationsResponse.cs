using System.Collections.Generic;
using CookbookApp.APi.Models;
namespace CookbookApp.APi.Models.DTO.Admin.Notification
{
    public class PagedNotificationsResponse
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
