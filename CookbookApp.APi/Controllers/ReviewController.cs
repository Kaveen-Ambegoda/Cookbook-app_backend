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
    public class ReviewController : ControllerBase
    {
        private readonly CookbookDbContext _context;

        public ReviewController(CookbookDbContext context)
        {
            _context = context;
        }

        // Everyone can view reviews
        [HttpGet("{recipeId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviews(int recipeId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.RecipeId == recipeId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    UserId = r.UserId
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // Only logged-in users can add
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest reviewRequest)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("User ID not found");

            if (reviewRequest.Rating < 1 || reviewRequest.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var review = new Review
            {
                RecipeId = reviewRequest.RecipeId,
                Rating = reviewRequest.Rating,
                Comment = reviewRequest.Comment,
                UserId = int.Parse(userIdClaim.Value),
                CreatedAt = DateTimeOffset.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(review);
        }

        // Only owner can edit
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> EditReview(int id, [FromBody] AddReviewRequest reviewRequest)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            if (review.UserId != int.Parse(userIdClaim.Value))
                return Forbid("Not your review");

            review.Rating = reviewRequest.Rating;
            review.Comment = reviewRequest.Comment;
            await _context.SaveChangesAsync();

            return Ok(review);
        }

        // Only owner can delete
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            if (review.UserId != int.Parse(userIdClaim.Value))
                return Forbid("Not your review");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok("Deleted");
        }

        [HttpGet("count/{recipeId}")]
        public async Task<IActionResult> GetReviewCount(int recipeId)
        {
            var count = await _context.Reviews.CountAsync(r => r.RecipeId == recipeId);
            return Ok(count);
        }

    }
}