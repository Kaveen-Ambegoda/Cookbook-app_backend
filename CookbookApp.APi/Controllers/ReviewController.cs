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
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    UserId = r.UserId,
                    Username = r.User.Username
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest reviewRequest)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("User ID not found");

            int reviewerId = int.Parse(userIdClaim.Value);

            // Check if recipe exists
            var recipe = await _context.Recipes
                .Include(r => r.User) // include the recipe owner
                .FirstOrDefaultAsync(r => r.Id == reviewRequest.RecipeId);

            if (recipe == null)
                return NotFound("Recipe not found");

            // Optional: Check if user already reviewed
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.RecipeId == reviewRequest.RecipeId && r.UserId == reviewerId);
            if (existingReview != null)
                return BadRequest("You have already reviewed this recipe.");

            // Create new review
            var newReview = new Review
            {
                RecipeId = reviewRequest.RecipeId,
                UserId = reviewerId,
                Rating = reviewRequest.Rating,
                Comment = reviewRequest.Comment
            };

            _context.Reviews.Add(newReview);

            // 👇 Create a notification for the recipe owner (if not reviewing their own recipe)
            if (recipe.UserID != reviewerId)
            {
                var notification = new Notification
                {
                    UserId = recipe.UserID,
                    Type = "Review",
                    Title = "New Review",
                    Message = $"Your recipe \"{recipe.Title}\" received a new review.",
                    RecipeId = recipe.Id,
                    ActionUrl = $"/RecipeManagement/review-page/{recipe.Id}",
                    ActionText = "View Review"
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Review added successfully." });
        }


        /*
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest reviewRequest)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("User ID not found");

            int userId = int.Parse(userIdClaim.Value);

            // Check review limit
            int reviewCount = await _context.Reviews
                .CountAsync(r => r.RecipeId == reviewRequest.RecipeId && r.UserId == userId);
            if (reviewCount >= 3)
                return BadRequest("You can only add up to 3 reviews for this recipe.");

            if (reviewRequest.Rating < 1 || reviewRequest.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var review = new Review
            {
                RecipeId = reviewRequest.RecipeId,
                Rating = reviewRequest.Rating,
                Comment = reviewRequest.Comment,
                UserId = userId,
                CreatedAt = DateTimeOffset.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // 🛑 Don’t block response on notification error
            _ = Task.Run(async () =>
            {
                try
                {
                    var recipe = await _context.Recipes
                        .Include(r => r.User)
                        .FirstOrDefaultAsync(r => r.Id == reviewRequest.RecipeId);

                    if (recipe != null && recipe.UserID != userId)
                    {
                        var reviewer = await _context.Users.FindAsync(userId);

                        if (reviewer != null && recipe.User != null)
                        {
                            var notification = new Notification
                            {
                                UserId = recipe.UserID,
                                Type = "review",
                                Title = "New Review on Your Recipe",
                                Message = $"{reviewer.Username} left a review on your recipe \"{recipe.Title}\".",
                                RecipeId = recipe.Id,
                                ActionUrl = $"/RecipeManagement/review-page/{recipe.Id}",
                                ActionText = "View Review",
                                CreatedAt = DateTimeOffset.UtcNow
                            };

                            _context.Notifications.Add(notification);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Notification Error] {ex.Message}");
                    // Optionally log to a file or telemetry
                }
            });

            return Ok(review);
        }
        */

        /*
        // Only logged-in users can add
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest reviewRequest)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("User ID not found");

            int userId = int.Parse(userIdClaim.Value);

            // LIMIT: Max 3 reviews per user per recipe
            int reviewCount = await _context.Reviews
                .CountAsync(r => r.RecipeId == reviewRequest.RecipeId && r.UserId == userId);
            if (reviewCount >= 3)
                return BadRequest("You can only add up to 3 reviews for this recipe.");

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
        }*/

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


        //Average Rating & Star Breakdown Endpoint

        [HttpGet("summary/{recipeId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewSummary(int recipeId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.RecipeId == recipeId)
                .ToListAsync();

            if (reviews.Count == 0)
                return Ok(new { average = 0, total = 0, breakdown = new int[5] });

            var average = reviews.Average(r => r.Rating);
            var total = reviews.Count;

            // Index 0 = 1-star, Index 4 = 5-star
            var breakdown = new int[5];
            foreach (var review in reviews)
            {
                if (review.Rating >= 1 && review.Rating <= 5)
                    breakdown[review.Rating - 1]++;
            }

            return Ok(new
            {
                average = Math.Round(average, 1),
                total,
                breakdown // [oneStar, twoStar, ..., fiveStar]
            });
        }

    }
}