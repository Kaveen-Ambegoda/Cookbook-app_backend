using CookbookApp.APi.Data;
using CookbookApp.APi.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly CookbookDbContext _context;

        public NotificationsController(CookbookDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(int page = 1, int pageSize = 10)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .Include(n => n.User)
                .Include(n => n.Recipe)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ActionUrl = n.ActionUrl,
                ActionText = n.ActionText,
                Username = n.User?.Username,
                RecipeName = n.Recipe?.Title,
                RecipeImage = n.Recipe?.Image
            }).ToList();

            return Ok(result);
        }

    }
}
