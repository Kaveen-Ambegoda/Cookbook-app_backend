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

        
    }
}
