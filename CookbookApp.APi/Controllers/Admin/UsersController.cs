using CookbookApp.APi.Data;
using CookbookApp.APi.Models.DTO.Admin.User;
using CookbookAppBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;

namespace CookbookApp.APi.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(CookbookDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Restrictions)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                var userDtos = users.Select(MapToDto).ToList();
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return StatusCode(500, new { message = "An error occurred while fetching users" });
            }
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Restrictions)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while fetching the user" });
            }
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            try
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                // Validate role
                var validRoles = new[] { "normal", "host", "admin" };
                if (!validRoles.Contains(createUserDto.Role))
                {
                    return BadRequest(new { message = "Invalid role specified" });
                }

                var user = new User
                {
                    Username = createUserDto.Name,
                    Email = createUserDto.Email,
                    PasswordHash = HashPassword(createUserDto.Password),
                    Role = createUserDto.Role,
                    Status = "active",
                    RegisteredDate = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create default restrictions
                var restrictions = new UserRestrictions
                {
                    UserId = user.Id,
                    Commenting = false,
                    Liking = false,
                    Posting = false,
                    Messaging = false,
                    LiveStreaming = false
                };

                _context.UserRestrictions.Add(restrictions);
                await _context.SaveChangesAsync();

                // Reload user with restrictions
                user = await _context.Users
                    .Include(u => u.Restrictions)
                    .FirstAsync(u => u.Id == user.Id);

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "An error occurred while creating the user" });
            }
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Check email uniqueness if email is being updated
                if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email && u.Id != id))
                    {
                        return BadRequest(new { message = "Email already exists" });
                    }
                    user.Email = updateUserDto.Email;
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateUserDto.Name))
                    user.Username = updateUserDto.Name;

                if (!string.IsNullOrEmpty(updateUserDto.Role))
                {
                    var validRoles = new[] { "normal", "host", "admin" };
                    if (!validRoles.Contains(updateUserDto.Role))
                    {
                        return BadRequest(new { message = "Invalid role specified" });
                    }
                    user.Role = updateUserDto.Role;
                }

                if (!string.IsNullOrEmpty(updateUserDto.Status))
                {
                    var validStatuses = new[] { "active", "banned", "restricted" };
                    if (!validStatuses.Contains(updateUserDto.Status))
                    {
                        return BadRequest(new { message = "Invalid status specified" });
                    }
                    user.Status = updateUserDto.Status;
                }

                // Update optional fields
                if (updateUserDto.Avatar != null) user.Avatar = updateUserDto.Avatar;
                if (updateUserDto.SubscriptionType != null) user.SubscriptionType = updateUserDto.SubscriptionType;
                if (updateUserDto.SubscriptionEnd.HasValue) user.SubscriptionEnd = updateUserDto.SubscriptionEnd;
                if (updateUserDto.LiveVideos.HasValue) user.LiveVideos = updateUserDto.LiveVideos.Value;
                if (updateUserDto.Posts.HasValue) user.Posts = updateUserDto.Posts.Value;
                if (updateUserDto.Events.HasValue) user.Events = updateUserDto.Events.Value;
                if (updateUserDto.Followers.HasValue) user.Followers = updateUserDto.Followers.Value;
                if (updateUserDto.Likes.HasValue) user.Likes = updateUserDto.Likes.Value;
                if (updateUserDto.Comments.HasValue) user.Comments = updateUserDto.Comments.Value;
                if (updateUserDto.VideosWatched.HasValue) user.VideosWatched = updateUserDto.VideosWatched.Value;
                if (updateUserDto.EngagementScore.HasValue) user.EngagementScore = CalculateEngagementScore(user);
                

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the user" });
            }
        }

        // PUT: api/users/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, UpdateUserStatusDto statusDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var validStatuses = new[] { "active", "banned", "restricted" };
                if (!validStatuses.Contains(statusDto.Status))
                {
                    return BadRequest(new { message = "Invalid status specified" });
                }

                user.Status = statusDto.Status;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating user status" });
            }
        }

        // PUT: api/users/5/role
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, UpdateUserRoleDto roleDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var validRoles = new[] { "normal", "host", "admin" };
                if (!validRoles.Contains(roleDto.Role))
                {
                    return BadRequest(new { message = "Invalid role specified" });
                }

                user.Role = roleDto.Role;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating user role" });
            }
        }

        // PUT: api/users/5/restrictions
        [HttpPut("{id}/restrictions")]
        public async Task<IActionResult> UpdateUserRestrictions(int id, UpdateUserRestrictionsDto restrictionsDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Restrictions)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (user.Restrictions == null)
                {
                    // Create restrictions if they don't exist
                    user.Restrictions = new UserRestrictions
                    {
                        UserId = id,
                        Commenting = restrictionsDto.Commenting,
                        Liking = restrictionsDto.Liking,
                        Posting = restrictionsDto.Posting,
                        Messaging = restrictionsDto.Messaging,
                        LiveStreaming = restrictionsDto.LiveStreaming
                    };
                    _context.UserRestrictions.Add(user.Restrictions);
                }
                else
                {
                    // Update existing restrictions
                    user.Restrictions.Commenting = restrictionsDto.Commenting;
                    user.Restrictions.Liking = restrictionsDto.Liking;
                    user.Restrictions.Posting = restrictionsDto.Posting;
                    user.Restrictions.Messaging = restrictionsDto.Messaging;
                    user.Restrictions.LiveStreaming = restrictionsDto.LiveStreaming;
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user restrictions for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating user restrictions" });
            }
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Restrictions)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Remove restrictions first due to foreign key constraint
                if (user.Restrictions != null)
                {
                    _context.UserRestrictions.Remove(user.Restrictions);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the user" });
            }
        }

        // GET: api/users/analytics
        [HttpGet("analytics")]
        public async Task<ActionResult<object>> GetUserAnalytics()
        {
            try
            {
                var users = await _context.Users.ToListAsync();

                var analytics = new
                {
                    totalUsers = users.Count,
                    totalHosts = users.Count(u => u.Role == "host"),
                    activeUsers = users.Count(u => u.Status == "active"),
                    bannedUsers = users.Count(u => u.Status == "banned"),
                    restrictedUsers = users.Count(u => u.Status == "restricted"),
                    reportedUsers = users.Count(u => u.Reported),
                    topUsers = users
                        .Where(u => u.Role == "normal")
                        .OrderByDescending(u => u.EngagementScore)
                        .Take(5)
                        .Select(MapToDto),
                    topHosts = users
                        .Where(u => u.Role == "host")
                        .OrderByDescending(u => u.Followers)
                        .Take(5)
                        .Select(MapToDto)
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user analytics");
                return StatusCode(500, new { message = "An error occurred while fetching analytics" });
            }
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Username,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,
                RegisteredDate = user.RegisteredDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                LastActive = user.LastActive.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ReportCount = user.ReportCount,
                ReportReason = user.ReportReason,
                Reported = user.Reported,
                Avatar = user.Avatar,
                SubscriptionType = user.SubscriptionType,
                SubscriptionEnd = user.SubscriptionEnd?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                LiveVideos = user.LiveVideos,
                Posts = user.Posts,
                Events = user.Events,
                Followers = user.Followers,
                Likes = user.Likes,
                Comments = user.Comments,
                VideosWatched = user.VideosWatched,
                EngagementScore = user.EngagementScore,
                Restrictions = user.Restrictions != null ? new UserRestrictionsDto
                {
                    Commenting = user.Restrictions.Commenting,
                    Liking = user.Restrictions.Liking,
                    Posting = user.Restrictions.Posting,
                    Messaging = user.Restrictions.Messaging,
                    LiveStreaming = user.Restrictions.LiveStreaming
                } : null
            };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private static int CalculateEngagementScore(User user)
        {
            return (user.Likes * 1) +
                   (user.Followers * 2) +
                   (user.Posts * 3) +
                   (user.Comments * 2) +
                   (user.Events * 5) +
                   (user.LiveVideos * 5) +
                   (user.VideosWatched * 1);
        }
    }
}
