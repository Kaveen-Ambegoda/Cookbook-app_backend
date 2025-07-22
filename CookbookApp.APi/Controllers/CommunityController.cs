// Location: Controllers/CommunityController.cs

using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommunityController : ControllerBase
    {
        private readonly CookbookDbContext dbContext;
        private readonly IWebHostEnvironment webHostEnvironment;

        public CommunityController(CookbookDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            this.dbContext = dbContext;
            this.webHostEnvironment = webHostEnvironment;
        }

        private (int UserId, string Username) GetCurrentUser()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            return (int.TryParse(userIdStr, out var id) ? id : 0, username);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetForums(
            [FromQuery] string? searchQuery = null,
            [FromQuery] string? author = null,
            [FromQuery] string? category = null,
            [FromQuery] bool showFavorites = false,
            [FromQuery] bool showMyForums = false)
        {
            var (userId, _) = GetCurrentUser();
            if (userId == 0) return Unauthorized("Invalid user token.");

            var query = dbContext.Forums
                .Include(f => f.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(f => f.Title.ToLower().Contains(searchQuery.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(author) && author != "All")
            {
                query = query.Where(f => f.Author.Username == author);
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(f => f.Category == category);
            }

            if (showMyForums)
            {
                query = query.Where(f => f.AuthorId == userId);
            }

            if (showFavorites)
            {
                var favoriteForumIds = await dbContext.UserFavorites
                    .Where(uf => uf.UserId == userId)
                    .Select(uf => uf.ForumId)
                    .ToListAsync();
                query = query.Where(f => favoriteForumIds.Contains(f.Id));
            }

            var forums = await query.ToListAsync();

            // --- THIS IS THE CORRECTED CODE BLOCK ---
            var favoriteIdsList = await dbContext.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Select(uf => uf.ForumId)
                .ToListAsync();
            var userFavorites = new HashSet<Guid>(favoriteIdsList);
            // --- END OF CORRECTION ---

            var userVotes = await dbContext.ForumVotes
                .Where(v => v.UserId == userId)
                .ToDictionaryAsync(v => v.ForumId, v => v.VoteType);

            var forumDtos = forums.Select(forum => new ForumDto
            {
                Id = forum.Id,
                Title = forum.Title,
                Image = $"/forum-images/{forum.Image}",
                Url = forum.Url,
                Timestamp = GetTimestampString(forum.CreatedAt),
                Comments = forum.CommentsCount,
                Views = forum.ViewsCount,
                Upvotes = forum.UpvotesCount,
                Downvotes = forum.DownvotesCount,
                Author = forum.Author.Username,
                Category = forum.Category,
                UserId = forum.AuthorId,
                IsFavorite = userFavorites.Contains(forum.Id),
                UserVote = userVotes.ContainsKey(forum.Id) ? (userVotes[forum.Id] == 1 ? "upvote" : "downvote") : null
            }).ToList();

            return Ok(forumDtos);
        }

        [HttpPost("{id}/view")]
        [Authorize]
        public async Task<IActionResult> IncrementView(Guid id)
        {
            var forum = await dbContext.Forums.FindAsync(id);
            if (forum == null) return NotFound();

            forum.ViewsCount++;
            await dbContext.SaveChangesAsync();

            return Ok(new { views = forum.ViewsCount });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateForum([FromForm] CreateForumRequestDto requestDto)
        {
            var (userId, username) = GetCurrentUser();
            if (userId == 0) return Unauthorized();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            string uniqueFileName = "default-forum.png";
            if (requestDto.Image != null)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "forum-images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string extension = Path.GetExtension(requestDto.Image.FileName);
                uniqueFileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await requestDto.Image.CopyToAsync(fileStream);
                }
            }

            var user = await dbContext.Users.FindAsync(userId);
            if (user == null) return Unauthorized("User not found.");

            var forum = new Forum
            {
                Id = Guid.NewGuid(),
                Title = requestDto.Title,
                Image = uniqueFileName,
                Url = requestDto.Url,
                CreatedAt = DateTime.UtcNow,
                Category = requestDto.Category,
                AuthorId = userId,
                Author = user
            };

            dbContext.Forums.Add(forum);
            await dbContext.SaveChangesAsync();

            return Ok(new ForumDto
            {
                Id = forum.Id,
                Title = forum.Title,
                Image = $"/forum-images/{forum.Image}",
                Url = forum.Url,
                Timestamp = GetTimestampString(forum.CreatedAt),
                Author = username,
                Category = forum.Category,
                IsFavorite = false,
                UserId = userId,
            });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateForum(Guid id, [FromForm] UpdateForumRequestDto requestDto)
        {
            var (userId, _) = GetCurrentUser();
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null) return NotFound();
            if (forum.AuthorId != userId) return Forbid("You can only edit your own forums.");

            if (requestDto.Image != null)
            {
                if (!string.IsNullOrEmpty(forum.Image) && forum.Image != "default-forum.png")
                {
                    string oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, "forum-images", forum.Image);
                    if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                }

                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "forum-images");
                string extension = Path.GetExtension(requestDto.Image.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await requestDto.Image.CopyToAsync(fileStream);
                }
                forum.Image = uniqueFileName;
            }

            forum.Title = requestDto.Title;
            forum.Url = requestDto.Url;
            forum.Category = requestDto.Category;

            await dbContext.SaveChangesAsync();

            var updatedForum = await dbContext.Forums.Include(f => f.Author).FirstOrDefaultAsync(f => f.Id == id);
            if (updatedForum == null) return NotFound();

            return Ok(new ForumDto
            {
                Id = updatedForum.Id,
                Title = updatedForum.Title,
                Image = $"/forum-images/{updatedForum.Image}",
                Url = updatedForum.Url,
                Category = updatedForum.Category,
                UserId = userId
            });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteForum(Guid id)
        {
            var (userId, _) = GetCurrentUser();
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null) return NotFound();
            if (forum.AuthorId != userId) return Forbid("You can only delete your own forums.");

            if (!string.IsNullOrEmpty(forum.Image) && forum.Image != "default-forum.png")
            {
                string imagePath = Path.Combine(webHostEnvironment.WebRootPath, "forum-images", forum.Image);
                if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
            }

            dbContext.Forums.Remove(forum);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        private async Task<VoteResponseDto?> UpdateVote(Guid forumId, int voteType)
        {
            var (userId, _) = GetCurrentUser();
            if (userId == 0) return null;

            var forum = await dbContext.Forums.FindAsync(forumId);
            if (forum == null) return null;

            var existingVote = await dbContext.ForumVotes
                .FirstOrDefaultAsync(v => v.ForumId == forumId && v.UserId == userId);

            string? finalUserVote = null;

            if (existingVote != null)
            {
                if (existingVote.VoteType == voteType)
                {
                    dbContext.ForumVotes.Remove(existingVote);
                }
                else
                {
                    existingVote.VoteType = voteType;
                    finalUserVote = voteType == 1 ? "upvote" : "downvote";
                }
            }
            else
            {
                dbContext.ForumVotes.Add(new ForumVote { ForumId = forumId, UserId = userId, VoteType = voteType });
                finalUserVote = voteType == 1 ? "upvote" : "downvote";
            }

            await dbContext.SaveChangesAsync();

            forum.UpvotesCount = await dbContext.ForumVotes.CountAsync(v => v.ForumId == forumId && v.VoteType == 1);
            forum.DownvotesCount = await dbContext.ForumVotes.CountAsync(v => v.ForumId == forumId && v.VoteType == -1);
            await dbContext.SaveChangesAsync();

            return new VoteResponseDto
            {
                Upvotes = forum.UpvotesCount,
                Downvotes = forum.DownvotesCount,
                UserVote = finalUserVote
            };
        }

        [HttpPost("{id}/upvote")]
        [Authorize]
        public async Task<IActionResult> UpvoteForum(Guid id)
        {
            var result = await UpdateVote(id, 1);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("{id}/downvote")]
        [Authorize]
        public async Task<IActionResult> DownvoteForum(Guid id)
        {
            var result = await UpdateVote(id, -1);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(Guid id)
        {
            var (userId, _) = GetCurrentUser();
            if (userId == 0) return Unauthorized();

            var favorite = await dbContext.UserFavorites
                .FirstOrDefaultAsync(uf => uf.ForumId == id && uf.UserId == userId);

            if (favorite != null)
            {
                dbContext.UserFavorites.Remove(favorite);
                await dbContext.SaveChangesAsync();
                return Ok(new { isFavorite = false });
            }
            else
            {
                dbContext.UserFavorites.Add(new UserFavorite { ForumId = id, UserId = userId });
                await dbContext.SaveChangesAsync();
                return Ok(new { isFavorite = true });
            }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await dbContext.Forums
                .Select(f => f.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            return Ok(categories);
        }

        [HttpGet("authors")]
        public async Task<IActionResult> GetAuthors()
        {
            var authors = await dbContext.Users
                .Where(u => dbContext.Forums.Any(f => f.AuthorId == u.Id))
                .Select(u => u.Username)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();
            return Ok(authors);
        }

        private string GetTimestampString(DateTime createdAt)
        {
            var timeSpan = DateTime.UtcNow - createdAt;
            if (timeSpan.TotalSeconds < 60) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
            return createdAt.ToString("MMM dd, yyyy");
        }
    }
}