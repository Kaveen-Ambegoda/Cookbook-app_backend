using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
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

        public SubmissionController(CookbookDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            this.dbContext = dbContext;
            this.webHostEnvironment = webHostEnvironment;
        }

        // POST: api/submission
        [HttpPost]
        public async Task<IActionResult> CreateSubmission([FromForm] CreateSubmissionRequestDto requestDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Handle image upload
                string? uniqueFileName = null;
                if (requestDto.RecipeImage != null)
                {
                    string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "submission-images");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Validate file size (5MB limit)
                    if (requestDto.RecipeImage.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest("File size must be less than 5MB");
                    }

                    // Validate file type
                    var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
                    var fileExtension = Path.GetExtension(requestDto.RecipeImage.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest("Only PNG, JPG, JPEG, GIF files are supported");
                    }

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + requestDto.RecipeImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await requestDto.RecipeImage.CopyToAsync(fileStream);
                    }
                }

                // Filter out empty ingredients
                var validIngredients = requestDto.Ingredients.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();

                var submission = new Submission
                {
                    Id = Guid.NewGuid(),
                    DisplayName = requestDto.DisplayName,
                    RecipeName = requestDto.RecipeName,
                    Ingredients = JsonSerializer.Serialize(validIngredients),
                    RecipeDescription = requestDto.RecipeDescription,
                    RecipeImage = uniqueFileName,
                    ChallengeId = requestDto.ChallengeId,
                    ChallengeName = requestDto.ChallengeName,
                    ChallengeCategory = requestDto.ChallengeCategory,
                    UserEmail = requestDto.UserEmail,
                    UserFullName = requestDto.UserFullName,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                dbContext.Submissions.Add(submission);
                await dbContext.SaveChangesAsync();

                var submissionDto = new SubmissionDto
                {
                    Id = submission.Id,
                    DisplayName = submission.DisplayName,
                    RecipeName = submission.RecipeName,
                    Ingredients = validIngredients,
                    RecipeDescription = submission.RecipeDescription,
                    RecipeImage = submission.RecipeImage != null ? $"/api/submission/image/{submission.RecipeImage}" : null,
                    ChallengeId = submission.ChallengeId,
                    ChallengeName = submission.ChallengeName,
                    ChallengeCategory = submission.ChallengeCategory,
                    UserEmail = submission.UserEmail,
                    UserFullName = submission.UserFullName,
                    CreatedAt = submission.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Status = submission.Status,
                    IsApproved = submission.IsApproved,
                    VotesCount = submission.VotesCount,
                    HasUserVoted = false
                };

                return Ok(new { success = true, message = "Recipe submitted successfully!", submission = submissionDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while submitting the recipe.", error = ex.Message });
            }
        }

        // GET: api/submission
        [HttpGet]
        public async Task<IActionResult> GetSubmissions([FromQuery] string? challengeId = null, [FromQuery] string? status = null, [FromQuery] string? userEmail = null)
        {
            try
            {
                var query = dbContext.Submissions.Include(s => s.Votes).Include(s => s.Ratings).AsQueryable();

                if (!string.IsNullOrEmpty(challengeId))
                {
                    query = query.Where(s => s.ChallengeId == challengeId);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(s => s.Status == status);
                }

                var submissions = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();

                var submissionDtos = submissions.Select(submission => new SubmissionDto
                {
                    Id = submission.Id,
                    DisplayName = submission.DisplayName,
                    RecipeName = submission.RecipeName,
                    Ingredients = JsonSerializer.Deserialize<List<string>>(submission.Ingredients) ?? new List<string>(),
                    RecipeDescription = submission.RecipeDescription,
                    RecipeImage = submission.RecipeImage != null ? $"/api/submission/image/{submission.RecipeImage}" : null,
                    ChallengeId = submission.ChallengeId,
                    ChallengeName = submission.ChallengeName,
                    ChallengeCategory = submission.ChallengeCategory,
                    UserEmail = submission.UserEmail,
                    UserFullName = submission.UserFullName,
                    CreatedAt = submission.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Status = submission.Status,
                    IsApproved = submission.IsApproved,
                    VotesCount = submission.Votes.Count,
                    AverageRating = submission.Ratings.Any() ? submission.Ratings.Average(r => r.Stars) : 0,
                    HasUserVoted = !string.IsNullOrEmpty(userEmail) && submission.Votes.Any(v => v.UserEmail == userEmail),
                    UserRating = !string.IsNullOrEmpty(userEmail) ? submission.Ratings.FirstOrDefault(r => r.UserEmail == userEmail)?.Stars : null
                }).ToList();

                return Ok(submissionDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching submissions.", error = ex.Message });
            }
        }

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

        // GET: api/submission/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubmission(Guid id)
        {
            try
            {
                var submission = await dbContext.Submissions.Include(s => s.Votes).FirstOrDefaultAsync(s => s.Id == id);
                
                if (submission == null)
                {
                    return NotFound(new { success = false, message = "Submission not found." });
                }

                var submissionDto = new SubmissionDto
                {
                    Id = submission.Id,
                    DisplayName = submission.DisplayName,
                    RecipeName = submission.RecipeName,
                    Ingredients = JsonSerializer.Deserialize<List<string>>(submission.Ingredients) ?? new List<string>(),
                    RecipeDescription = submission.RecipeDescription,
                    RecipeImage = submission.RecipeImage != null ? $"/api/submission/image/{submission.RecipeImage}" : null,
                    ChallengeId = submission.ChallengeId,
                    ChallengeName = submission.ChallengeName,
                    ChallengeCategory = submission.ChallengeCategory,
                    UserEmail = submission.UserEmail,
                    UserFullName = submission.UserFullName,
                    CreatedAt = submission.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Status = submission.Status,
                    IsApproved = submission.IsApproved,
                    VotesCount = submission.VotesCount,
                    HasUserVoted = false
                };

                return Ok(submissionDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching the submission.", error = ex.Message });
            }
        }
    }
}
