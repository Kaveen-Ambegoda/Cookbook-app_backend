using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
                ChallengeId = dto.ChallengeId,
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
        

        // GET: api/submission/image/{filename}
        [HttpGet("image/{filename}")]
        public IActionResult GetImage(string filename)
        {
            var imagePath = Path.Combine(webHostEnvironment.WebRootPath, "submission-images", filename);
            
            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound();
            }

            var image = System.IO.File.OpenRead(imagePath);
            return File(image, "image/jpeg");
        }

        
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
    }

    // You need a DTO for model binding:
    
}
