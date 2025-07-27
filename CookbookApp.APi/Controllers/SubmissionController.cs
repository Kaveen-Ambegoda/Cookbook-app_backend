using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CookbookAppBackend.Models; 


namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionController : ControllerBase
    {
        private readonly CookbookDbContext dbContext;
        
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly Cloudinary _cloudinary;

        public SubmissionController(CookbookDbContext dbContext, IWebHostEnvironment webHostEnvironment, Cloudinary cloudinary)
        {
            this.dbContext = dbContext;
            this.webHostEnvironment = webHostEnvironment;
            _cloudinary = cloudinary;
        }

        // POST: api/submission
        [HttpPost]
        public async Task<IActionResult> Submit([FromForm] SubmissionCreateDto dto)
        {
            // Get user info from JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            var userEmailClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email);

            if (userIdClaim == null || userEmailClaim == null)
                return Unauthorized(new { success = false, message = "User info missing in token." });

            // Handle image upload
            string? imageUrl = null;
            if (dto.RecipeImage != null)
            {
                using (var stream = dto.RecipeImage.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(dto.RecipeImage.FileName, stream),
                        Folder = "submission-images"
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        imageUrl = uploadResult.SecureUrl?.ToString();
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "Image upload failed." });
                    }
                }
            }

            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                FullName = dto.UserFullName,
                RecipeName = dto.RecipeName,
                RecipeDescription = dto.RecipeDescription,
                ChallengeId = dto.ChallengeId, // <-- Add this line
                ChallengeName = dto.ChallengeName,
                ChallengeCategory = dto.ChallengeCategory,
                Ingredients = System.Text.Json.JsonSerializer.Serialize(dto.Ingredients),
                RecipeImage = imageUrl,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                IsApproved = false,
                UserId = int.Parse(userIdClaim.Value),
                UserEmail = userEmailClaim.Value
            };

            dbContext.Submissions.Add(submission);
            await dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "Submission created.", submissionId = submission.Id });
        }

     // GET: api/submission/voteAndRate   
        

        
        
        // POST: api/submission/approve/{id}
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveSubmission(Guid id)
        {
            var submission = await dbContext.Submissions.FindAsync(id);
            if (submission == null)
                return NotFound(new { success = false, message = "Submission not found." });

            submission.IsApproved = true;
            submission.Status = "Approved";
            submission.ApprovedAt = DateTime.UtcNow;
            submission.ApprovedBy = "ManualTest"; // or any test value

            await dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "Submission approved manually for testing." });
        }

        [HttpGet("challenge/{challengeId}")]
        public async Task<IActionResult> GetSubmissionsByChallengeId(string challengeId)
        {
            var submissions = await dbContext.Submissions
                .Where(s => s.ChallengeId == challengeId)
                .Include(s => s.User)
                .ToListAsync();

            var result = submissions.Select(s => new
            {
                SubmissionId = s.Id,
                FullName = s.FullName,
                RecipeName = s.RecipeName,
                Ingredients = JsonSerializer.Deserialize<List<string>>(s.Ingredients),
                RecipeDescription = s.RecipeDescription,
                RecipeImage = s.RecipeImage,
                ChallengeCategory = s.ChallengeCategory,
                Votes = s.Votes,
                // Calculate average rating and total ratings from Ratings table
                rating = dbContext.Ratings.Where(r => r.SubmissionId == s.Id).Any()
                    ? dbContext.Ratings.Where(r => r.SubmissionId == s.Id).Average(r => r.Stars)
                    : 0,
                totalRatings = dbContext.Ratings.Count(r => r.SubmissionId == s.Id)
            }).ToList();

            return Ok(result);
        }

        [HttpGet("challenge/{challengeId}/recipes")]
        public async Task<IActionResult> GetRecipesByChallengeId(string challengeId)
        {
            var recipes = await dbContext.Submissions
                .Where(s => s.ChallengeId == challengeId)
                .Select(s => new
                {
                    submissionId = s.Id, // Fixed: Use 'Id' instead of 'SubmissionId'
                    recipeName = s.RecipeName,
                    // ...other fields...
                    averageRating = dbContext.Ratings.Where(r => r.SubmissionId == s.Id).Any() // Fixed: Use 'Id' instead of 'SubmissionId'
                        ? dbContext.Ratings.Where(r => r.SubmissionId == s.Id).Average(r => r.Stars)
                        : 0,
                    totalRatings = dbContext.Ratings.Count(r => r.SubmissionId == s.Id) // Fixed: Use 'Id' instead of 'SubmissionId'
                })
                .ToListAsync();

            return Ok(recipes);
        }

        [HttpGet("challenge/{challengeId}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(string challengeId)
        {
            // Get all submissions for the challenge
            var submissions = await dbContext.Submissions
                .Where(s => s.ChallengeId == challengeId)
                .ToListAsync();

            // Get all ratings for these submissions
            var ratings = await dbContext.Ratings
                .Where(r => r.ChallengeId == challengeId)
                .ToListAsync();

            // Calculate C (global average rating across all recipes in this challenge)
            double C = ratings.Any() ? ratings.Average(r => r.Stars) : 0;

            // Choose m (minimum ratings before trusting average fully)
            int m = 10;

            // Prepare leaderboard entries
            var leaderboard = submissions.Select(s =>
            {
                var submissionRatings = ratings.Where(r => r.SubmissionId == s.Id).ToList();
                int v = submissionRatings.Count;
                double R = v > 0 ? submissionRatings.Average(r => r.Stars) : 0;

                // Bayesian average ranking
                double weightedRating = (v / (double)(v + m)) * R + (m / (double)(v + m)) * C;

                return new
                {
                    id = s.Id,
                    name = s.FullName,
                    recipeName = s.RecipeName,
                    recipeImage = s.RecipeImage,
                    recipeDescription = s.RecipeDescription,
                    score = Math.Round(weightedRating * 20, 2), // scale to 100 if you want
                    votes = s.Votes,
                    rating = Math.Round(R, 2),
                    totalRatings = v,
                    challengeCategory = s.ChallengeCategory
                };
            })
            .OrderByDescending(e => e.score)
            .ToList();

            
            // Assign ranks
            int rank = 1;
            var rankedLeaderboard = leaderboard.Select(e => new
            {
                e.id,
                e.name,
                e.recipeName,
                e.recipeImage,
                e.recipeDescription,
                e.score,
                rank = rank++, // Add rank property
                e.votes,
                e.rating,
                e.totalRatings,
                e.challengeCategory
            }).ToList();

            return Ok(leaderboard);
        }

    }

    // You need a DTO for model binding:
    
}
