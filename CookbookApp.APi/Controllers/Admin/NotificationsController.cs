using Microsoft.AspNetCore.Mvc;
using CookbookApp.APi.Services;
using CookbookApp.APi.Models.DTO.Admin.Notification;
using CookbookApp.APi.Models;
using CookbookApp.APi.Models.DTO.Admin;
namespace CookbookApp.APi.Controllers.Admin
{
  
    
        [ApiController]
        [Route("api/admin/[controller]")]
        public class NotificationsController : ControllerBase
        {
            private readonly INotificationService _svc;

            public NotificationsController(INotificationService svc)
            {
                _svc = svc;
            }

            // GET /api/admin/notifications
            [HttpGet]
            public async Task<ActionResult<PagedNotificationsResponse>> GetNotifications([FromQuery] NotificationQuery query)
            {
                var result = await _svc.GetPagedAsync(query);
                return Ok(result);
            }

            // GET /api/admin/notifications/{id}
            [HttpGet("{id:guid}")]
            public async Task<ActionResult<NotificationDto>> GetNotification(Guid id)
            {
                var n = await _svc.GetByIdAsync(id);
                if (n is null) return NotFound();
                return Ok(n);
            }

            public record UpdateStatusRequest(string? Action, string? Status);
            // PATCH /api/admin/notifications/{id}/status
            [HttpPatch("{id:guid}/status")]
            public async Task<ActionResult<NotificationDto>> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
            {
                // Accept Action (approve/reject/resolve/dismiss) OR direct Status enum string
                NotificationStatus status;
                if (!string.IsNullOrWhiteSpace(req.Status) &&
                    Enum.TryParse<NotificationStatus>(req.Status, ignoreCase: true, out var parsedStatus))
                {
                    status = parsedStatus;
                }
                else
                {
                    status = req.Action?.ToLowerInvariant() switch
                    {
                        "approve" => NotificationStatus.Approved,
                        "reject" => NotificationStatus.Rejected,
                        "resolve" => NotificationStatus.Resolved,
                        "dismiss" => NotificationStatus.Dismissed,
                        _ => NotificationStatus.Pending
                    };
                }

                var n = await _svc.UpdateStatusAsync(id, status);
                if (n is null) return NotFound();
                return Ok(n);
            }

            public record MarkReadRequest(bool IsRead);
            // PATCH /api/admin/notifications/{id}/read
            [HttpPatch("{id:guid}/read")]
            public async Task<ActionResult<NotificationDto>> MarkRead(Guid id, [FromBody] MarkReadRequest? req)
            {
                var isRead = req?.IsRead ?? true;
                var n = await _svc.MarkReadAsync(id, isRead);
                if (n is null) return NotFound();
                return Ok(n);
            }

            // POST /api/admin/notifications/mark-all-read
            [HttpPost("mark-all-read")]
            public async Task<ActionResult<object>> MarkAllRead()
            {
                var count = await _svc.MarkAllReadAsync();
                return Ok(new { updated = count });
            }

            // GET /api/admin/notifications/stats
            [HttpGet("stats")]
            public async Task<ActionResult<NotificationStatsDto>> GetStats()
            {
                var s = await _svc.GetStatsAsync();
                return Ok(s);
            }
        }
    }


