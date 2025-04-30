using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : ControllerBase
    {
        private readonly CookbookDbContext dbContext;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ForumController(CookbookDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            this.dbContext = dbContext;
            this.webHostEnvironment = webHostEnvironment;
        }

        // GET: api/forum
        [HttpGet]
        public async Task<IActionResult> GetForums(
            [FromQuery] string? searchQuery = null,
            [FromQuery] string? author = null,
            [FromQuery] string? category = null,
            [FromQuery] bool showFavorites = false,
            [FromQuery] bool showMyForums = false,
            [FromQuery] string userId = "CurrentUser") // Default user ID for demo
        {
            var query = dbContext.Forums.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(f => f.Title.Contains(searchQuery));
            }

            if (!string.IsNullOrWhiteSpace(author) && author != "All")
            {
                query = query.Where(f => f.AuthorId == author);
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(f => f.Category == category);
            }

            if (showMyForums)
            {
                query = query.Where(f => f.AuthorId == userId);
            }

            // Get user favorites to check favorite status
            var userFavorites = await dbContext.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Select(uf => uf.ForumId)
                .ToListAsync();

            if (showFavorites)
            {
                query = query.Where(f => userFavorites.Contains(f.Id));
            }

            var forums = await query.ToListAsync();

            // Map to DTOs
            var forumDtos = forums.Select(forum => new ForumDto
            {
                Id = forum.Id,
                Title = forum.Title,
                Image = $"/api/forum/image/{forum.Image}",
                Url = forum.Url,
                Timestamp = GetTimestampString(forum.CreatedAt),
                Comments = forum.CommentsCount,
                Views = forum.ViewsCount,
                Upvotes = forum.UpvotesCount,
                Downvotes = forum.DownvotesCount,
                Author = forum.AuthorId,
                Category = forum.Category,
                IsFavorite = userFavorites.Contains(forum.Id)
            }).ToList();

            return Ok(forumDtos);
        }

        // GET: api/forum/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetForum(Guid id, [FromQuery] string userId = "CurrentUser")
        {
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            // Increment view count
            forum.ViewsCount++;
            await dbContext.SaveChangesAsync();

            // Check if user has favorited this forum
            var isFavorite = await dbContext.UserFavorites
                .AnyAsync(uf => uf.ForumId == id && uf.UserId == userId);

            var forumDto = new ForumDto
            {
                Id = forum.Id,
                Title = forum.Title,
                Image = $"/api/forum/image/{forum.Image}",
                Url = forum.Url,
                Timestamp = GetTimestampString(forum.CreatedAt),
                Comments = forum.CommentsCount,
                Views = forum.ViewsCount,
                Upvotes = forum.UpvotesCount,
                Downvotes = forum.DownvotesCount,
                Author = forum.AuthorId,
                Category = forum.Category,
                IsFavorite = isFavorite
            };

            return Ok(forumDto);
        }

        // POST: api/forum
        [HttpPost]
        public async Task<IActionResult> CreateForum([FromForm] CreateForumRequestDto requestDto, [FromQuery] string userId = "CurrentUser")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Process image file
            string uniqueFileName = null;
            if (requestDto.Image != null)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "forum-images");

                // Ensure directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                uniqueFileName = Guid.NewGuid().ToString() + "_" + requestDto.Image.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await requestDto.Image.CopyToAsync(fileStream);
                }
            }

            // Create new forum
            var forum = new Forum
            {
                Id = Guid.NewGuid(),
                Title = requestDto.Title,
                Image = uniqueFileName ?? "default-forum.jpg", // Default image if none provided
                Url = requestDto.Url,
                CreatedAt = DateTime.UtcNow,
                CommentsCount = 0,
                ViewsCount = 0,
                UpvotesCount = 0,
                DownvotesCount = 0,
                AuthorId = userId,
                Category = requestDto.Category
            };

            dbContext.Forums.Add(forum);
            await dbContext.SaveChangesAsync();

            var forumDto = new ForumDto
            {
                Id = forum.Id,
                Title = forum.Title,
                Image = $"/api/forum/image/{forum.Image}",
                Url = forum.Url,
                Timestamp = GetTimestampString(forum.CreatedAt),
                Comments = forum.CommentsCount,
                Views = forum.ViewsCount,
                Upvotes = forum.UpvotesCount,
                Downvotes = forum.DownvotesCount,
                Author = forum.AuthorId,
                Category = forum.Category,
                IsFavorite = false
            };

            return CreatedAtAction(nameof(GetForum), new { id = forum.Id }, forumDto);
        }

        // PUT: api/forum/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateForum(Guid id, [FromForm] CreateForumRequestDto requestDto)
        {
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            // Process image file if provided
            if (requestDto.Image != null)
            {
                // Delete old image if it exists and is not the default
                if (!string.IsNullOrEmpty(forum.Image) && forum.Image != "default-forum.jpg")
                {
                    string oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, "forum-images", forum.Image);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "forum-images");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + requestDto.Image.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await requestDto.Image.CopyToAsync(fileStream);
                }

                forum.Image = uniqueFileName;
            }

            // Update other properties
            forum.Title = requestDto.Title;
            forum.Url = requestDto.Url;
            forum.Category = requestDto.Category;

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/forum/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForum(Guid id)
        {
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            // Delete associated image if it's not the default
            if (!string.IsNullOrEmpty(forum.Image) && forum.Image != "default-forum.jpg")
            {
                string imagePath = Path.Combine(webHostEnvironment.WebRootPath, "forum-images", forum.Image);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            dbContext.Forums.Remove(forum);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/forum/image/{filename}
        [HttpGet("image/{filename}")]
        public IActionResult GetImage(string filename)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "forum-images", filename);

            if (!System.IO.File.Exists(path))
            {
                // Return default image if requested image doesn't exist
                path = Path.Combine(webHostEnvironment.WebRootPath, "forum-images", "default-forum.jpg");

                // If even default image doesn't exist, return not found
                if (!System.IO.File.Exists(path))
                {
                    return NotFound();
                }
            }

            var image = System.IO.File.OpenRead(path);
            return File(image, "image/jpeg"); // Adjust content type as needed
        }

        // POST: api/forum/{id}/upvote
        [HttpPost("{id}/upvote")]
        public async Task<IActionResult> UpvoteForum(Guid id)
        {
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            forum.UpvotesCount++;
            await dbContext.SaveChangesAsync();

            return Ok(new { upvotes = forum.UpvotesCount });
        }

        // POST: api/forum/{id}/downvote
        [HttpPost("{id}/downvote")]
        public async Task<IActionResult> DownvoteForum(Guid id)
        {
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            forum.DownvotesCount++;
            await dbContext.SaveChangesAsync();

            return Ok(new { downvotes = forum.DownvotesCount });
        }

        // POST: api/forum/{id}/favorite
        [HttpPost("{id}/favorite")]
        public async Task<IActionResult> ToggleFavorite(Guid id, [FromQuery] string userId = "CurrentUser")
        {
            var forum = await dbContext.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            // Check if user has already favorited this forum
            var favorite = await dbContext.UserFavorites
                .FirstOrDefaultAsync(uf => uf.ForumId == id && uf.UserId == userId);

            if (favorite != null)
            {
                // Remove favorite
                dbContext.UserFavorites.Remove(favorite);
                await dbContext.SaveChangesAsync();
                return Ok(new { isFavorite = false });
            }
            else
            {
                // Add favorite
                var userFavorite = new UserFavorite
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ForumId = id
                };

                dbContext.UserFavorites.Add(userFavorite);
                await dbContext.SaveChangesAsync();
                return Ok(new { isFavorite = true });
            }
        }

        // GET: api/forum/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await dbContext.Forums
                .Select(f => f.Category)
                .Distinct()
                .ToListAsync();

            // Add "All" category
            categories.Insert(0, "All");

            return Ok(categories);
        }

        // GET: api/forum/authors
        [HttpGet("authors")]
        public async Task<IActionResult> GetAuthors()
        {
            var authors = await dbContext.Forums
                .Select(f => f.AuthorId)
                .Distinct()
                .ToListAsync();

            // Add "All" option
            authors.Insert(0, "All");

            return Ok(authors);
        }

        // Helper method to format timestamp
        private string GetTimestampString(DateTime createdAt)
        {
            var timeSpan = DateTime.UtcNow - createdAt;

            if (timeSpan.TotalSeconds < 60)
            {
                return "Just now";
            }
            if (timeSpan.TotalMinutes < 60)
            {
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            }
            if (timeSpan.TotalHours < 24)
            {
                return $"{(int)timeSpan.TotalHours} hours ago";
            }
            if (timeSpan.TotalDays < 7)
            {
                return $"{(int)timeSpan.TotalDays} days ago";
            }

            return createdAt.ToString("MMM dd, yyyy");
        }
    }
}