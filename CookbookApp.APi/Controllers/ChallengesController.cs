using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Text.Json;
using CookbookApp.APi.Models;

namespace CookbookApp.APi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChallengesController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly Cloudinary _cloudinary;

        public ChallengesController(
            CookbookDbContext context,
            IWebHostEnvironment hostingEnvironment,
            Cloudinary cloudinary)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _cloudinary = cloudinary;
        }

        // ✅ Your existing challenge submission method
        [HttpPost]
        public async Task<IActionResult> AddChallenge([FromBody] AddChallengeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var challenge = new Challenge
                {
                    FullName = dto.FullName,
                    ChallengeCategory = dto.ChallengeCategory,
                    ReasonForChoosing = dto.ReasonForChoosing,
                    TermsAccepted = dto.TermsAccepted,
                    ChallengeName = dto.ChallengeName
                };

                _context.Challenges.Add(challenge);
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

                return BadRequest(new { error = "An error occurred while processing your request." });
            }
        }

        // ✅ NEW: Add detailed challenge with Cloudinary image upload
        [HttpPost("add-details")]
        public async Task<IActionResult> AddChallengeDetails([FromForm] AddChallengeDetailDto request)
        {
            if (request.Image == null || request.Image.Length == 0)
                return BadRequest("Image is required.");

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(request.Image.FileName, request.Image.OpenReadStream())
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                var challengeDetail = new ChallengeDetail
                {
                    Title = request.Title,
                    Subtitle = request.Subtitle,
                    Date = request.Date,
                    Sponsor = request.Sponsor,
                    ImgUrl = uploadResult.SecureUrl.ToString(),
                    Description = request.Description,
                    Requirements = JsonSerializer.Serialize(request.Requirements),
                    TimelineRegistration = request.TimelineRegistration,
                    TimelineJudging = request.TimelineJudging,
                    TimelineWinnersAnnounced = request.TimelineWinnersAnnounced
                };

                _context.ChallengeDetails.Add(challengeDetail);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Challenge details added successfully", id = challengeDetail.Id });
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

                return BadRequest(new { error = "Failed to add challenge details." });
            }
        }

        // ✅ NEW: Get challenge details
        [HttpGet("details")]
        public async Task<IActionResult> GetChallengeDetails()
        {
            var details = await _context.ChallengeDetails
                .Select(cd => new ChallengeDetailListDto
                {
                    Id = cd.Id,
                    Title = cd.Title,
                    Subtitle = cd.Subtitle,
                    Img = cd.ImgUrl,
                    Date = cd.Date,
                    Sponsor = cd.Sponsor
                })
                .ToListAsync();

            return Ok(details);
        }

        // ✅ NEW: Get challenge detail by ID
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetChallengeDetail(int id)
        {
            var challenge = await _context.ChallengeDetails.FindAsync(id);
            if (challenge == null)
                return NotFound();

            var dto = new ChallengeDetailDto
            {
                Id = challenge.Id,
                Title = challenge.Title,
                ChallengeCategory = challenge.Subtitle, // or another property if you have a category field
                Description = challenge.Description,
                Timeline = new TimelineDto
                {
                    Registration = challenge.TimelineRegistration,
                    Judging = challenge.TimelineJudging,
                    WinnersAnnounced = challenge.TimelineWinnersAnnounced
                },
                Requirements = challenge.Requirements?.Split(',').Select(r => r.Trim()).ToList() ?? new List<string>()
            };

            return Ok(dto);
        }

        
    }
}
