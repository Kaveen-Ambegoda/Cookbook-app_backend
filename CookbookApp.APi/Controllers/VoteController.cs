using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoteController : ControllerBase
    {
        private readonly CookbookDbContext dbContext;

        public VoteController(CookbookDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // POST: api/vote
        [HttpPost]
        public async Task<IActionResult> Vote([FromBody] VoteRequestDto requestDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already voted for this submission
                var existingVote = await dbContext.Votes
                    .FirstOrDefaultAsync(v => v.UserEmail == requestDto.UserEmail && v.SubmissionId == requestDto.SubmissionId);

                if (existingVote != null)
                {
                    // Remove existing vote (toggle)
                    dbContext.Votes.Remove(existingVote);
                    await dbContext.SaveChangesAsync();
                    return Ok(new { success = true, message = "Vote removed successfully!", voted = false });
                }

                // Add new vote
                var vote = new Vote
                {
                    UserEmail = requestDto.UserEmail,
                    SubmissionId = requestDto.SubmissionId,
                    ChallengeId = requestDto.ChallengeId,
                    VotedAt = DateTime.UtcNow
                };

                dbContext.Votes.Add(vote);
                await dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Vote added successfully!", voted = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while voting.", error = ex.Message });
            }
        }

        // POST: api/vote/rate
        [HttpPost("rate")]
        public async Task<IActionResult> Rate([FromBody] RatingRequestDto requestDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already rated this submission
                var existingRating = await dbContext.Ratings
                    .FirstOrDefaultAsync(r => r.UserEmail == requestDto.UserEmail && r.SubmissionId == requestDto.SubmissionId);

                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Stars = requestDto.Stars;
                    existingRating.RatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Add new rating
                    var rating = new Rating
                    {
                        UserEmail = requestDto.UserEmail,
                        SubmissionId = requestDto.SubmissionId,
                        ChallengeId = requestDto.ChallengeId,
                        Stars = requestDto.Stars,
                        RatedAt = DateTime.UtcNow
                    };

                    dbContext.Ratings.Add(rating);
                }

                await dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Rating submitted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while rating.", error = ex.Message });
            }
        }
    }
}
