using CookbookApp.APi.Data;
using CookbookApp.APi.Models;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using CookbookApp.API.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ChallengeController : Controller
    {
        private readonly CookbookDbContext _context;

        public ChallengeController(CookbookDbContext dbContext)
        {
            this._context = dbContext;
        }
        [HttpPost("join")]
        public IActionResult JoinChallenge([FromBody] JoinChallengeDto joinChallengeDto)
        {
            // Validate the input
            if (joinChallengeDto == null || string.IsNullOrEmpty(joinChallengeDto.ChallengeId))
            {
                return BadRequest("Invalid challenge data.");
            }
            // Map DTO to domain model
            var challengeParticipant = new ChallengeParticipant
            {
                Id = Guid.NewGuid(),
                ChallengeId = joinChallengeDto.ChallengeId,
                FullName = joinChallengeDto.FullName,
                Email = joinChallengeDto.Email,
                Category = joinChallengeDto.Category,
                Motivation = joinChallengeDto.Motivation,
                JoinDate = DateTime.UtcNow
            };
            // Save to the database
            _context.ChallengeParticipants.Add(challengeParticipant);
            _context.SaveChanges();
            return Ok("Successfully joined the challenge.");
        }

    }
}




    
       