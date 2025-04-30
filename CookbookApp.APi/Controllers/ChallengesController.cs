using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookbookApp.APi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChallengesController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        public ChallengesController(CookbookDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddChallenge([FromBody] Challenge challenge)
        {
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
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}