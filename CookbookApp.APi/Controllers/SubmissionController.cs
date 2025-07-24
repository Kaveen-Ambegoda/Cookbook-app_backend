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
                .Where(s => s.ChallengeName == challengeId)
                .Include(s => s.User) // to access FullName
                .ToListAsync(); // Ensure the query is executed asynchronously

            var result = submissions.Select(s => new
            {
                SubmissionId = s.Id,
                FullName = s.User?.Username, // Use Username instead of FullName
                RecipeName = s.RecipeName,
                Ingredients = JsonSerializer.Deserialize<List<string>>(s.Ingredients),
                RecipeDescription = s.RecipeDescription,
                RecipeImage = s.RecipeImage,
                ChallengeCategory = s.ChallengeCategory, // Use ChallengeCategory directly from Submission
                Votes = s.Votes,
                Rating = s.Rating,
                TotalRatings = s.TotalRatings
            }).ToList();

            return Ok(result);
        }



    }

    // You need a DTO for model binding:
    
}
