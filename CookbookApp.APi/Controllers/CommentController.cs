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
    [Route("api/community")] // Changed route for consistency
    public class CommentController : ControllerBase
    {
        private readonly CookbookDbContext dbContext;

        public CommentController(CookbookDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        private (int UserId, string Username) GetCurrentUser()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            return (int.TryParse(userIdStr, out var id) ? id : 0, username);
        }

        [HttpGet("forums/{forumId}/comments")]
        public async Task<IActionResult> GetCommentsForForum(Guid forumId)
        {
            var comments = await dbContext.Comments
                .Where(c => c.ForumId == forumId)
                .Include(c => c.Replies)
                .OrderBy(c => c.CreatedAt)
                .Select(comment => new CommentDto
                {
                    Id = comment.Id,
                    ForumId = comment.ForumId,
                    UserId = comment.UserId,
                    Username = comment.Username,
                    Content = comment.Content,
                    Timestamp = GetTimestampString(comment.CreatedAt),
                    Replies = comment.Replies.OrderBy(r => r.CreatedAt).Select(reply => new ReplyDto
                    {
                        Id = reply.Id,
                        CommentId = reply.CommentId,
                        UserId = reply.UserId,
                        Username = reply.Username,
                        Content = reply.Content,
                        Timestamp = GetTimestampString(reply.CreatedAt)
                    }).ToList()
                }).ToListAsync();

            return Ok(comments);
        }

        [HttpPost("forums/{forumId}/comments")]
        [Authorize]
        public async Task<IActionResult> AddComment(Guid forumId, [FromBody] AddCommentRequestDto requestDto)
        {
            var (userId, username) = GetCurrentUser();
            if (userId == 0) return Unauthorized();

            var forum = await dbContext.Forums.FindAsync(forumId);
            if (forum == null) return NotFound("Forum not found");

            var comment = new Comment
            {
                ForumId = forumId,
                UserId = userId,
                Username = username,
                Content = requestDto.Content,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Comments.Add(comment);
            forum.CommentsCount++;
            await dbContext.SaveChangesAsync();

            return Ok(new CommentDto
            {
                Id = comment.Id,
                ForumId = comment.ForumId,
                UserId = comment.UserId,
                Username = comment.Username,
                Content = comment.Content,
                Timestamp = GetTimestampString(comment.CreatedAt),
                Replies = new List<ReplyDto>()
            });
        }

        [HttpPost("comments/{commentId}/replies")]
        [Authorize]
        public async Task<IActionResult> AddReply(Guid commentId, [FromBody] AddReplyRequestDto requestDto)
        {
            var (userId, username) = GetCurrentUser();
            if (userId == 0) return Unauthorized();

            var comment = await dbContext.Comments.Include(c => c.Forum).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null) return NotFound("Comment not found");

            var reply = new Reply
            {
                CommentId = commentId,
                UserId = userId,
                Username = username,
                Content = requestDto.Content,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Replies.Add(reply);
            comment.Forum.CommentsCount++;
            await dbContext.SaveChangesAsync();

            return Ok(new ReplyDto
            {
                Id = reply.Id,
                CommentId = reply.CommentId,
                UserId = reply.UserId,
                Username = reply.Username,
                Content = reply.Content,
                Timestamp = GetTimestampString(reply.CreatedAt)
            });
        }

        [HttpDelete("comments/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var (userId, _) = GetCurrentUser();
            var comment = await dbContext.Comments
                .Include(c => c.Replies)
                .Include(c => c.Forum)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return NotFound();
            if (comment.UserId != userId) return Forbid();

            int itemsToRemove = 1 + comment.Replies.Count;
            comment.Forum.CommentsCount = Math.Max(0, comment.Forum.CommentsCount - itemsToRemove);

            dbContext.Comments.Remove(comment); // Cascade will delete replies
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("replies/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReply(Guid id)
        {
            var (userId, _) = GetCurrentUser();
            var reply = await dbContext.Replies
                .Include(r => r.Comment.Forum)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reply == null) return NotFound();
            if (reply.UserId != userId) return Forbid();

            reply.Comment.Forum.CommentsCount = Math.Max(0, reply.Comment.Forum.CommentsCount - 1);

            dbContext.Replies.Remove(reply);
            await dbContext.SaveChangesAsync();

            return NoContent();
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