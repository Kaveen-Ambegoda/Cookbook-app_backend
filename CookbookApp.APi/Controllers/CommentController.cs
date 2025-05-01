using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly CookbookDbContext dbContext;

        public CommentController(CookbookDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // GET: api/comment/forum/{forumId}
        [HttpGet("forum/{forumId}")]
        public async Task<IActionResult> GetComments(Guid forumId)
        {
            var forum = await dbContext.Forums.FindAsync(forumId);
            if (forum == null)
            {
                return NotFound("Forum not found");
            }

            var comments = await dbContext.Comments
                .Where(c => c.ForumId == forumId)
                .Include(c => c.Replies)
                .ToListAsync();

            var commentDtos = comments.Select(comment => new CommentDto
            {
                Id = comment.Id,
                ForumId = comment.ForumId,
                UserId = comment.UserId,
                Username = comment.Username,
                Content = comment.Content,
                Timestamp = GetTimestampString(comment.CreatedAt),
                Replies = comment.Replies.Select(reply => new ReplyDto
                {
                    Id = reply.Id,
                    CommentId = reply.CommentId,
                    UserId = reply.UserId,
                    Username = reply.Username,
                    Content = reply.Content,
                    Timestamp = GetTimestampString(reply.CreatedAt)
                }).ToList()
            }).ToList();

            return Ok(commentDtos);
        }

        // POST: api/comment/forum/{forumId}
        [HttpPost("forum/{forumId}")]
        public async Task<IActionResult> AddComment(Guid forumId, [FromBody] AddCommentRequestDto requestDto, [FromQuery] string userId = "CurrentUser")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var forum = await dbContext.Forums.FindAsync(forumId);
            if (forum == null)
            {
                return NotFound("Forum not found");
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                ForumId = forumId,
                UserId = userId,
                Username = userId, // In a real app, get the actual username from user service
                Content = requestDto.Content,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Comments.Add(comment);

            // Increment comment count on forum
            forum.CommentsCount++;

            await dbContext.SaveChangesAsync();

            var commentDto = new CommentDto
            {
                Id = comment.Id,
                ForumId = comment.ForumId,
                UserId = comment.UserId,
                Username = comment.Username,
                Content = comment.Content,
                Timestamp = GetTimestampString(comment.CreatedAt),
                Replies = new List<ReplyDto>()
            };

            return CreatedAtAction(nameof(GetComments), new { forumId }, commentDto);
        }

        // POST: api/comment/{commentId}/reply
        [HttpPost("{commentId}/reply")]
        public async Task<IActionResult> AddReply(Guid commentId, [FromBody] AddReplyRequestDto requestDto, [FromQuery] string userId = "CurrentUser")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var comment = await dbContext.Comments.Include(c => c.Forum).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
            {
                return NotFound("Comment not found");
            }

            var reply = new Reply
            {
                Id = Guid.NewGuid(),
                CommentId = commentId,
                UserId = userId,
                Username = userId, // In a real app, get the actual username from user service
                Content = requestDto.Content,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Replies.Add(reply);

            // Increment comment count on forum (replies count as comments too)
            comment.Forum.CommentsCount++;

            await dbContext.SaveChangesAsync();

            var replyDto = new ReplyDto
            {
                Id = reply.Id,
                CommentId = reply.CommentId,
                UserId = reply.UserId,
                Username = reply.Username,
                Content = reply.Content,
                Timestamp = GetTimestampString(reply.CreatedAt)
            };

            return CreatedAtAction(nameof(GetComments), new { forumId = comment.ForumId }, replyDto);
        }

        // DELETE: api/comment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var comment = await dbContext.Comments
                .Include(c => c.Replies)
                .Include(c => c.Forum)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            // Calculate how many items we're removing (comment + all replies)
            int itemsToRemove = 1 + comment.Replies.Count;

            // Update forum comment count
            comment.Forum.CommentsCount -= itemsToRemove;

            dbContext.Comments.Remove(comment);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/comment/reply/{id}
        [HttpDelete("reply/{id}")]
        public async Task<IActionResult> DeleteReply(Guid id)
        {
            var reply = await dbContext.Replies
                .Include(r => r.Comment)
                .ThenInclude(c => c.Forum)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reply == null)
            {
                return NotFound();
            }

            // Update forum comment count
            reply.Comment.Forum.CommentsCount--;

            dbContext.Replies.Remove(reply);
            await dbContext.SaveChangesAsync();

            return NoContent();
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
    