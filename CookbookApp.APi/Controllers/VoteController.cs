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

                // Check if user already voted for this submission in this challenge
                var existingVote = await dbContext.Votes
                    .FirstOrDefaultAsync(v => v.UserEmail == requestDto.UserEmail
                        && v.SubmissionId == requestDto.SubmissionId
                        && v.ChallengeId == requestDto.ChallengeId);

                if (existingVote != null)
                {
                    return BadRequest(new { success = false, message = "You have already voted for this recipe.", voted = false });
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

                var submission = await dbContext.Submissions.FindAsync(requestDto.SubmissionId);
                if (submission != null)
                {
                    submission.Votes += 1;
                }

                await dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Vote added successfully!", voted = true, votes = submission?.Votes ?? 0 });
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

        // GET: api/vote/user
        [HttpGet("user")]
        public async Task<IActionResult> GetUserVotes([FromQuery] string userEmail, [FromQuery] int challengeId)
        {
            var votes = await dbContext.Votes
                .Where(v => v.UserEmail == userEmail && v.ChallengeId == challengeId.ToString())
                .Select(v => new { submissionId = v.SubmissionId })
                .ToListAsync();

            return Ok(votes);
        }

        // GET: api/vote/raters?challengeId=xxx
        [HttpGet("raters")]
        public async Task<IActionResult> GetRaters([FromQuery] string challengeId)
        {
            var raters = await dbContext.Ratings
                .Where(r => r.ChallengeId == challengeId)
                .Select(r => r.UserEmail)
                .Distinct()
                .ToListAsync();

            return Ok(new { count = raters.Count, users = raters });
        }

        // GET: api/vote/user-ratings?userEmail=xxx&challengeId=xxx
        [HttpGet("user-ratings")]
        public async Task<IActionResult> GetUserRatings([FromQuery] string userEmail, [FromQuery] string challengeId)
        {
            var ratings = await dbContext.Ratings
                .Where(r => r.UserEmail == userEmail && r.ChallengeId == challengeId)
                .Select(r => new { submissionId = r.SubmissionId, stars = r.Stars })
                .ToListAsync();

            return Ok(ratings);
        }
    }
}
