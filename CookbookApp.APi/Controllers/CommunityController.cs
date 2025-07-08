using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CookbookAppBackend.Data;
using CookbookAppBackend.Models;
using System.Security.Claims;
using System.IO;

namespace CookbookAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommunityController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CommunityController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Community
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ForumDto>>> GetForums(
            [FromQuery] string? searchQuery,
            [FromQuery] string? author,
            [FromQuery] string? category,
            [FromQuery] bool showFavorites = false,
            [FromQuery] bool showMyForums = false,
            [FromQuery] string? userId = null,
            [FromQuery] string sortBy = "score")
        {
            var currentUserId = GetCurrentUserId();

            var query = _context.Forums
                .Include(f => f.User)
                .Include(f => f.Favorites)
                .Include(f => f.Comments)
                .Include(f => f.UserVotes)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(f => f.Title.Contains(searchQuery));
            }

            // Apply author filter
            if (!string.IsNullOrEmpty(author) && author != "All")
            {
                query = query.Where(f => f.User.Username == author);
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                query = query.Where(f => f.Category == category);
            }

            // Apply favorites filter
            if (showFavorites)
            {
                query = query.Where(f => f.Favorites.Any(fav => fav.UserId == currentUserId));
            }

            // Apply my forums filter
            if (showMyForums)
            {
                query = query.Where(f => f.UserId == currentUserId);
            }

            // Apply sorting based on sortBy parameter
            query = sortBy.ToLower() switch
            {
                "newest" => query.OrderByDescending(f => f.CreatedAt),
                "oldest" => query.OrderBy(f => f.CreatedAt),
                "views" => query.OrderByDescending(f => f.Views),
                "comments" => query.OrderByDescending(f => f.Comments.Count),
                "score" or _ => query.OrderByDescending(f => f.Upvotes - f.Downvotes)
                    .ThenByDescending(f => f.CreatedAt)
            };

            var forums = await query
                .Select(f => new ForumDto
                {
                    Id = f.Id.ToString(),
                    Title = f.Title,
                    Image = f.ImageUrl ?? "/image/cookbook_app.png",
                    Url = f.Url,
                    Timestamp = f.CreatedAt.ToString("MMM dd, yyyy"),
                    Comments = f.Comments.Count,
                    Views = f.Views,
                    Upvotes = f.Upvotes,
                    Downvotes = f.Downvotes,
                    Author = f.User.Username,
                    Category = f.Category,
                    IsFavorite = f.Favorites.Any(fav => fav.UserId == currentUserId),
                    UserId = f.UserId.ToString(),
                    UserVote = f.UserVotes
                        .Where(v => v.UserId == currentUserId)
                        .Select(v => v.VoteType == VoteType.Upvote ? "upvote" : "downvote")
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(forums);
        }

        // GET: api/Community/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ForumDto>> GetForum(int id)
        {
            var currentUserId = GetCurrentUserId();
            var forum = await _context.Forums
                .Include(f => f.User)
                .Include(f => f.Favorites)
                .Include(f => f.Comments)
                .Include(f => f.UserVotes)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (forum == null)
            {
                return NotFound();
            }

            // Increment view count
            forum.Views++;
            await _context.SaveChangesAsync();

            // Get user's vote for this forum
            var userVote = forum.UserVotes
                .FirstOrDefault(v => v.UserId == currentUserId);

            var forumDto = new ForumDto
            {
                Id = forum.Id.ToString(),
                Title = forum.Title,
                Image = forum.ImageUrl ?? "/image/cookbook_app.png",
                Url = forum.Url,
                Timestamp = forum.CreatedAt.ToString("MMM dd, yyyy"),
                Comments = forum.Comments.Count,
                Views = forum.Views,
                Upvotes = forum.Upvotes,
                Downvotes = forum.Downvotes,
                Author = forum.User.Username,
                Category = forum.Category,
                IsFavorite = forum.Favorites.Any(fav => fav.UserId == currentUserId),
                UserId = forum.UserId.ToString(),
                UserVote = userVote?.VoteType == VoteType.Upvote ? "upvote" :
                          userVote?.VoteType == VoteType.Downvote ? "downvote" : null
            };

            return Ok(forumDto);
        }

        // POST: api/Community/{id}/view
        [HttpPost("{id}/view")]
        public async Task<ActionResult> IncrementView(int id)
        {
            var forum = await _context.Forums.FindAsync(id);
            if (forum == null)
            {
                return NotFound();
            }

            forum.Views++;
            await _context.SaveChangesAsync();

            return Ok(new { views = forum.Views });
        }

        // POST: api/Community/{id}/upvote - Smart voting system
        [HttpPost("{id}/upvote")]
        public async Task<ActionResult<object>> Upvote(int id)
        {
            var currentUserId = GetCurrentUserId();

            var forum = await _context.Forums
                .Include(f => f.UserVotes)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (forum == null)
            {
                return NotFound();
            }

            // Check if user already voted
            var existingVote = await _context.UserVotes
                .FirstOrDefaultAsync(v => v.ForumId == id && v.UserId == currentUserId);

            if (existingVote != null)
            {
                if (existingVote.VoteType == VoteType.Upvote)
                {
                    // User is removing their upvote
                    _context.UserVotes.Remove(existingVote);
                    forum.Upvotes--;
                    Console.WriteLine($"User {currentUserId} removed upvote from forum {id}");
                }
                else
                {
                    // User is changing from downvote to upvote
                    existingVote.VoteType = VoteType.Upvote;
                    existingVote.UpdatedAt = DateTime.UtcNow;
                    forum.Downvotes--;
                    forum.Upvotes++;
                    Console.WriteLine($"User {currentUserId} changed vote from downvote to upvote on forum {id}");
                }
            }
            else
            {
                // User is voting for the first time
                var newVote = new UserVote
                {
                    ForumId = id,
                    UserId = currentUserId,
                    VoteType = VoteType.Upvote,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserVotes.Add(newVote);
                forum.Upvotes++;
                Console.WriteLine($"User {currentUserId} added new upvote to forum {id}");
            }

            await _context.SaveChangesAsync();

            // Get user's current vote status
            var currentVote = await _context.UserVotes
                .Where(v => v.ForumId == id && v.UserId == currentUserId)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                upvotes = forum.Upvotes,
                downvotes = forum.Downvotes,
                score = forum.Upvotes - forum.Downvotes,
                userVote = currentVote?.VoteType == VoteType.Upvote ? "upvote" :
                          currentVote?.VoteType == VoteType.Downvote ? "downvote" : null
            });
        }

        // POST: api/Community/{id}/downvote - Smart voting system
        [HttpPost("{id}/downvote")]
        public async Task<ActionResult<object>> Downvote(int id)
        {
            var currentUserId = GetCurrentUserId();

            var forum = await _context.Forums
                .Include(f => f.UserVotes)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (forum == null)
            {
                return NotFound();
            }

            // Check if user already voted
            var existingVote = await _context.UserVotes
                .FirstOrDefaultAsync(v => v.ForumId == id && v.UserId == currentUserId);

            if (existingVote != null)
            {
                if (existingVote.VoteType == VoteType.Downvote)
                {
                    // User is removing their downvote
                    _context.UserVotes.Remove(existingVote);
                    forum.Downvotes--;
                    Console.WriteLine($"User {currentUserId} removed downvote from forum {id}");
                }
                else
                {
                    // User is changing from upvote to downvote
                    existingVote.VoteType = VoteType.Downvote;
                    existingVote.UpdatedAt = DateTime.UtcNow;
                    forum.Upvotes--;
                    forum.Downvotes++;
                    Console.WriteLine($"User {currentUserId} changed vote from upvote to downvote on forum {id}");
                }
            }
            else
            {
                // User is voting for the first time
                var newVote = new UserVote
                {
                    ForumId = id,
                    UserId = currentUserId,
                    VoteType = VoteType.Downvote,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserVotes.Add(newVote);
                forum.Downvotes++;
                Console.WriteLine($"User {currentUserId} added new downvote to forum {id}");
            }

            await _context.SaveChangesAsync();

            // Get user's current vote status
            var currentVote = await _context.UserVotes
                .Where(v => v.ForumId == id && v.UserId == currentUserId)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                upvotes = forum.Upvotes,
                downvotes = forum.Downvotes,
                score = forum.Upvotes - forum.Downvotes,
                userVote = currentVote?.VoteType == VoteType.Upvote ? "upvote" :
                          currentVote?.VoteType == VoteType.Downvote ? "downvote" : null
            });
        }

        // Debug endpoint for troubleshooting
        [HttpGet("debug/forum/{id}")]
        public async Task<IActionResult> DebugForumOwnership(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var forum = await _context.Forums
                    .Include(f => f.User)
                    .Include(f => f.UserVotes)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (forum == null)
                {
                    return Ok(new
                    {
                        message = "Forum not found",
                        forumId = id,
                        exists = false
                    });
                }

                var userVote = forum.UserVotes.FirstOrDefault(v => v.UserId == currentUserId);

                return Ok(new
                {
                    message = "Forum debug info",
                    forumId = id,
                    exists = true,
                    forum = new
                    {
                        id = forum.Id,
                        title = forum.Title,
                        userId = forum.UserId,
                        author = forum.User?.Username,
                        upvotes = forum.Upvotes,
                        downvotes = forum.Downvotes,
                        totalVotes = forum.UserVotes.Count
                    },
                    currentUser = new
                    {
                        id = currentUserId,
                        vote = userVote?.VoteType.ToString(),
                        hasVoted = userVote != null
                    },
                    ownership = new
                    {
                        isOwner = forum.UserId == currentUserId
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Debug error",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/Community/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForum(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                Console.WriteLine($"Delete attempt - Forum ID: {id}, Current User ID: {currentUserId}");

                var forum = await _context.Forums
                    .Include(f => f.Comments)
                        .ThenInclude(c => c.Replies)
                    .Include(f => f.Favorites)
                    .Include(f => f.UserVotes)
                    .Include(f => f.User)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (forum == null)
                {
                    Console.WriteLine($"Forum with ID {id} not found");
                    return NotFound(new { message = "Forum not found" });
                }

                Console.WriteLine($"Forum found - Owner ID: {forum.UserId}, Current User ID: {currentUserId}");

                // Check if the current user owns this forum
                if (forum.UserId != currentUserId)
                {
                    Console.WriteLine($"Ownership check failed - Forum Owner: {forum.UserId}, Current User: {currentUserId}");
                    return StatusCode(403, new { message = "You can only delete your own forums" });
                }

                Console.WriteLine("Ownership verified, proceeding with deletion");

                // Delete associated image if it exists and is not the default
                if (!string.IsNullOrEmpty(forum.ImageUrl) && !forum.ImageUrl.Contains("/image/cookbook_app.png"))
                {
                    var imagePath = Path.Combine(_environment.ContentRootPath, "wwwroot", forum.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(imagePath);
                            Console.WriteLine($"Deleted image file: {imagePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete image file: {ex.Message}");
                        }
                    }
                }

                // Remove the forum (cascade delete will handle comments, replies, favorites, and votes)
                _context.Forums.Remove(forum);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Forum {id} deleted successfully");
                return Ok(new { message = "Forum deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete forum error: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "An error occurred while deleting the forum",
                    error = ex.Message
                });
            }
        }

        // POST: api/Community
        [HttpPost]
        public async Task<ActionResult<ForumDto>> CreateForum([FromForm] CreateForumRequest request)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required");
            }

            string? imageUrl = null;
            if (request.Image != null)
            {
                imageUrl = await SaveImageAsync(request.Image);
            }

            var forum = new Forum
            {
                Title = request.Title.Trim(),
                Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim(),
                Category = request.Category,
                ImageUrl = imageUrl,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                Views = 0,
                Upvotes = 0,
                Downvotes = 0
            };

            _context.Forums.Add(forum);
            await _context.SaveChangesAsync();

            // Load the forum with user data
            await _context.Entry(forum)
                .Reference(f => f.User)
                .LoadAsync();

            var forumDto = new ForumDto
            {
                Id = forum.Id.ToString(),
                Title = forum.Title,
                Image = forum.ImageUrl ?? "/image/cookbook_app.png",
                Url = forum.Url,
                Timestamp = forum.CreatedAt.ToString("MMM dd, yyyy"),
                Comments = 0,
                Views = forum.Views,
                Upvotes = forum.Upvotes,
                Downvotes = forum.Downvotes,
                Author = forum.User.Username,
                Category = forum.Category,
                IsFavorite = false,
                UserId = forum.UserId.ToString(),
                UserVote = null
            };

            return CreatedAtAction(nameof(GetForum), new { id = forum.Id }, forumDto);
        }

        // PUT: api/Community/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ForumDto>> UpdateForum(int id, [FromForm] CreateForumRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var forum = await _context.Forums
                .Include(f => f.User)
                .Include(f => f.UserVotes)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (forum == null)
            {
                return NotFound();
            }

            // Check if the current user owns this forum
            if (forum.UserId != currentUserId)
            {
                return StatusCode(403, new { message = "You can only edit your own forums" });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required");
            }

            // Update forum properties
            forum.Title = request.Title.Trim();
            forum.Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim();
            forum.Category = request.Category;
            forum.UpdatedAt = DateTime.UtcNow;

            // Handle image update
            if (request.Image != null)
            {
                // Delete old image if it exists and is not the default
                if (!string.IsNullOrEmpty(forum.ImageUrl) && !forum.ImageUrl.Contains("/image/cookbook_app.png"))
                {
                    var oldImagePath = Path.Combine(_environment.ContentRootPath, "wwwroot", forum.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                var newImageUrl = await SaveImageAsync(request.Image);
                forum.ImageUrl = newImageUrl;
            }

            await _context.SaveChangesAsync();

            // Get user's vote for this forum
            var userVote = forum.UserVotes.FirstOrDefault(v => v.UserId == currentUserId);

            var forumDto = new ForumDto
            {
                Id = forum.Id.ToString(),
                Title = forum.Title,
                Image = forum.ImageUrl ?? "/image/cookbook_app.png",
                Url = forum.Url,
                Timestamp = forum.CreatedAt.ToString("MMM dd, yyyy"),
                Comments = forum.Comments?.Count ?? 0,
                Views = forum.Views,
                Upvotes = forum.Upvotes,
                Downvotes = forum.Downvotes,
                Author = forum.User.Username,
                Category = forum.Category,
                IsFavorite = await _context.Favorites.AnyAsync(f => f.ForumId == id && f.UserId == currentUserId),
                UserId = forum.UserId.ToString(),
                UserVote = userVote?.VoteType == VoteType.Upvote ? "upvote" :
                          userVote?.VoteType == VoteType.Downvote ? "downvote" : null
            };

            return Ok(forumDto);
        }

        // POST: api/Community/{id}/favorite
        [HttpPost("{id}/favorite")]
        public async Task<ActionResult<object>> ToggleFavorite(int id)
        {
            var currentUserId = GetCurrentUserId();
            var forum = await _context.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ForumId == id && f.UserId == currentUserId);

            bool isFavorite;
            if (existingFavorite != null)
            {
                _context.Favorites.Remove(existingFavorite);
                isFavorite = false;
            }
            else
            {
                _context.Favorites.Add(new Favorite
                {
                    ForumId = id,
                    UserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                });
                isFavorite = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { isFavorite });
        }

        // GET: api/Community/{id}/comments
        [HttpGet("{id}/comments")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int id)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Where(c => c.ForumId == id)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id.ToString(),
                    ForumId = c.ForumId.ToString(),
                    UserId = c.UserId.ToString(),
                    Username = c.User.Username,
                    Content = c.Content,
                    Timestamp = c.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                    Replies = c.Replies.Select(r => new ReplyDto
                    {
                        Id = r.Id.ToString(),
                        CommentId = r.CommentId.ToString(),
                        UserId = r.UserId.ToString(),
                        Username = r.User.Username,
                        Content = r.Content,
                        Timestamp = r.CreatedAt.ToString("MMM dd, yyyy HH:mm")
                    }).ToList()
                })
                .ToListAsync();

            return Ok(comments);
        }

        // POST: api/Community/{id}/comments
        [HttpPost("{id}/comments")]
        public async Task<ActionResult<CommentDto>> AddComment(int id, [FromBody] CreateCommentRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var forum = await _context.Forums.FindAsync(id);

            if (forum == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required");
            }

            var comment = new Comment
            {
                ForumId = id,
                UserId = currentUserId,
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Load user data
            await _context.Entry(comment)
                .Reference(c => c.User)
                .LoadAsync();

            var commentDto = new CommentDto
            {
                Id = comment.Id.ToString(),
                ForumId = comment.ForumId.ToString(),
                UserId = comment.UserId.ToString(),
                Username = comment.User.Username,
                Content = comment.Content,
                Timestamp = comment.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                Replies = new List<ReplyDto>()
            };

            return CreatedAtAction(nameof(GetComments), new { id = id }, commentDto);
        }

        // POST: api/Community/comments/{commentId}/replies
        [HttpPost("comments/{commentId}/replies")]
        public async Task<ActionResult<ReplyDto>> AddReply(int commentId, [FromBody] CreateReplyRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required");
            }

            var reply = new Reply
            {
                CommentId = commentId,
                UserId = currentUserId,
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Replies.Add(reply);
            await _context.SaveChangesAsync();

            // Load user data
            await _context.Entry(reply)
                .Reference(r => r.User)
                .LoadAsync();

            var replyDto = new ReplyDto
            {
                Id = reply.Id.ToString(),
                CommentId = reply.CommentId.ToString(),
                UserId = reply.UserId.ToString(),
                Username = reply.User.Username,
                Content = reply.Content,
                Timestamp = reply.CreatedAt.ToString("MMM dd, yyyy HH:mm")
            };

            return Ok(replyDto);
        }

        // GET: api/Community/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.Forums
                .Select(f => f.Category)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .ToListAsync();

            var defaultCategories = new List<string> { "Recipes", "Tips", "Questions", "General", "Techniques", "Ingredients" };
            var allCategories = defaultCategories.Union(categories).ToList();

            return Ok(allCategories);
        }

        // GET: api/Community/authors
        [HttpGet("authors")]
        public async Task<ActionResult<IEnumerable<string>>> GetAuthors()
        {
            var authors = await _context.Forums
                .Include(f => f.User)
                .Select(f => f.User.Username)
                .Distinct()
                .ToListAsync();

            return Ok(authors);
        }

        // Debug endpoint to test JWT parsing
        [HttpGet("debug/user-info")]
        public IActionResult GetCurrentUserInfo()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var user = _context.Users.Find(currentUserId);

                var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();

                return Ok(new
                {
                    message = "User info retrieved successfully",
                    userId = currentUserId,
                    user = user != null ? new { user.Id, user.Username, user.Email } : null,
                    claims = claims,
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Failed to get user info",
                    error = ex.Message,
                    claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
                });
            }
        }

        // Helper methods
        private int GetCurrentUserId()
        {
            // Try to get from NameIdentifier claim (standard claim)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            // Try custom userId claim
            var customUserIdClaim = User.FindFirst("userId")?.Value;
            if (!string.IsNullOrEmpty(customUserIdClaim) && int.TryParse(customUserIdClaim, out int customUserId))
            {
                return customUserId;
            }

            // Fallback: Get user by email from the token
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(emailClaim))
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == emailClaim);
                if (user != null)
                {
                    return user.Id;
                }
            }

            throw new UnauthorizedAccessException("User ID not found in token");
        }

        private async Task<string?> SaveImageAsync(IFormFile image)
        {
            try
            {
                var contentRoot = _environment.ContentRootPath;
                var uploadsFolder = Path.Combine(contentRoot, "wwwroot", "forum-images");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                if (System.IO.File.Exists(filePath))
                {
                    var imageUrl = $"/forum-images/{uniqueFileName}";
                    return imageUrl;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
                return null;
            }
        }
    }
}