using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // Add this
using System.IO; // Add this
using System.Threading.Tasks; // Add this
using CookbookApp.APi.Models.DTO; // Add this

namespace CookbookApp.APi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChallengesController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment; // Add this

        // Update the constructor
        public ChallengesController(CookbookDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpPost]
        public async Task<IActionResult> AddChallenge([FromBody] Challenge challenge)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                // Fix: Correctly cast _context.Challenges to DbSet<Challenge>
                var challengesDbSet = _context.Set<Challenge>();
                challengesDbSet.Add(challenge);

                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new { message = "Challenge added successfully", challengeId = challenge.Id });
                }
                return BadRequest(new { error = "Failed to add challenge" });
            }
            catch (Exception ex)
            {
                if (_hostingEnvironment.IsDevelopment())
                {
                    return BadRequest(new
                    {
                        error = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                else
                {
                    return BadRequest(new { error = "An error occurred while processing your request." });
                }
            }
        }

        // Add new method for recipe submission
        [HttpPost("SubmitChallenge")]
        public async Task<IActionResult> SubmitRecipe([FromForm] RecipeSubmissionDto submission)
        {
            try
            {
                string imageUrl = "";
                try
                {
                    imageUrl = await HandleImageUpload(submission.Image);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { error = ex.Message }); // Handle image upload errors
                }

                // Create domain model
                var forumSubmission = new SubmitForum
                {
                    ChallengeId = submission.ChallengeId,
                    RecipeName = submission.RecipeName,
                    Ingredients = submission.Ingredients,
                    Description = submission.Description,
                    ImageUrl = imageUrl
                };

                // Add to database
                _context.SubmitForums.Add(forumSubmission);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Recipe submitted successfully",
                    submissionId = forumSubmission.Id
                });
            }
            catch (Exception ex)
            {
                if (_hostingEnvironment.IsDevelopment())
                {
                    return BadRequest(new
                    {
                        error = ex.Message,
                        innerException = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                else
                {
                    return BadRequest(new { error = "An error occurred while processing your request." });
                }
            }
        }

        private async Task<string> HandleImageUpload(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return string.Empty;

            if (image.Length > 5 * 1024 * 1024) // 5MB
            {
                throw new Exception("File size exceeds 5MB limit");
            }

            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLower();
            if (!validExtensions.Contains(extension))
            {
                throw new Exception("Invalid file type");
            }

            // Get web root path safely
            var webRootPath = _hostingEnvironment.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var imagesPath = Path.Combine(webRootPath, "images");
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            // Create unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(imagesPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/images/{fileName}";
        }
    }
}
