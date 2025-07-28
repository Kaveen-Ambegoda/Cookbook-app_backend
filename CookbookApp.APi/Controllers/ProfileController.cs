using CookbookApp.APi.Data;
using CookbookApp.APi.Models.DTO;
using CookbookApp.APi.Services;
using CookbookAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CookbookAppBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(CookbookDbContext context, ICloudinaryService cloudinary, ILogger<ProfileController> logger)
        {
            _context = context;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        // GET: /api/profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var user = await _context.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                    return NotFound("User not found");

                var profileDto = new UserProfileDto
                {
                    Username = user.Username ?? "Unknown User",
                    Email = user.Email ?? "",
                    Bio = user.Bio,
                    Location = user.Location,
                    PersonalLinks = user.PersonalLinks,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Status = user.Status ?? "inactive"
                };

                _logger.LogInformation("Profile fetched successfully for user {UserId}", userId);
                return Ok(profileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: /api/profile/update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updatedProfile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var user = await _context.Users
                    .SingleOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                    return NotFound("User not found");

                // Update only the fields that are allowed to be updated
                user.Bio = updatedProfile.Bio;
                user.Location = updatedProfile.Location;
                user.PersonalLinks = updatedProfile.PersonalLinks;

                // Only update profile picture URL if it's provided
                if (!string.IsNullOrEmpty(updatedProfile.ProfilePictureUrl))
                {
                    user.ProfilePictureUrl = updatedProfile.ProfilePictureUrl;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
                return Ok(new { message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: /api/profile/upload-image
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Invalid file" });

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "Only JPEG, PNG, and GIF images are allowed" });
                }

                // Validate file size (5MB limit)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File size cannot exceed 5MB" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var user = await _context.Users
                    .SingleOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                    return NotFound("User not found");

                // Upload image to Cloudinary
                var imageUrl = await _cloudinary.UploadImageAsync(file);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    return StatusCode(500, new { message = "Failed to upload image to cloud storage" });
                }

                // Update user's profile picture URL
                user.ProfilePictureUrl = imageUrl;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile image uploaded successfully for user {UserId}", userId);
                return Ok(new { url = imageUrl, message = "Image uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image");
                return StatusCode(500, new { message = "Internal server error occurred while uploading image" });
            }
        }

        // PUT: /api/profile/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
                {
                    return BadRequest(new { message = "Current password and new password are required" });
                }

                if (model.NewPassword.Length < 8)
                    return BadRequest(new { message = "Password must be at least 8 characters" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var user = await _context.Users
                    .SingleOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                    return NotFound("User not found");

                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                    return BadRequest(new { message = "Current password is incorrect" });

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: /api/profile/delete
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var user = await _context.Users
                    .SingleOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                    return NotFound("User not found");

                // Optional: Delete user's profile picture from Cloudinary
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    try
                    {
                        // Implement this method in your Cloudinary service if needed
                        // await _cloudinary.DeleteImageAsync(user.ProfilePictureUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete profile image from cloud storage for user {UserId}", userId);
                        // Continue with account deletion even if image deletion fails
                    }
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Account deleted successfully for user {UserId}", userId);
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account for user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    // Helper model with validation attributes
    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string NewPassword { get; set; } = string.Empty;
    }
}