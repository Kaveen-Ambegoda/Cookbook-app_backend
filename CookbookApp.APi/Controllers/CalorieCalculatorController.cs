using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using CookbookApp.APi.Services;
using CookbookAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class CalorieCalculatorController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        private readonly ICalorieCalculationService _calorieService;

        public CalorieCalculatorController(CookbookDbContext context, ICalorieCalculationService calorieService)
        {
            _context = context;
            _calorieService = calorieService;
        }

        // GET: api/CalorieCalculator/profile
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserProfileResponseDto>>> GetUserProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new ApiResponse<UserProfileResponseDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });

                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value);

                if (profile == null)
                {
                    return Ok(new ApiResponse<UserProfileResponseDto>
                    {
                        Success = true,
                        Message = "No profile found",
                        Data = null
                    });
                }

                var profileDto = MapToUserProfileDto(profile);

                return Ok(new ApiResponse<UserProfileResponseDto>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = profileDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<UserProfileResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the profile",
                    Errors = { ex.Message }
                });
            }
        }

        // POST: api/CalorieCalculator/calculate
        [HttpPost("calculate")]
        public async Task<ActionResult<ApiResponse<CalorieCalculationResponseDto>>> CalculateCalories([FromBody] UserProfileRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<CalorieCalculationResponseDto>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new ApiResponse<CalorieCalculationResponseDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });

                // Create or update user profile
                var profile = await CreateOrUpdateUserProfile(userId.Value, request);

                // Calculate calories
                var calculation = _calorieService.CalculateCalories(profile);
                calculation.UserProfile = profile;

                // Save calculation to database
                _context.CalorieCalculations.Add(calculation);
                await _context.SaveChangesAsync();

                // Map to response DTO
                var responseDto = MapToCalorieCalculationDto(calculation);

                return Ok(new ApiResponse<CalorieCalculationResponseDto>
                {
                    Success = true,
                    Message = "Calories calculated successfully",
                    Data = responseDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CalorieCalculationResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while calculating calories",
                    Errors = { ex.Message }
                });
            }
        }

        // GET: api/CalorieCalculator/latest
        [HttpGet("latest")]
        public async Task<ActionResult<ApiResponse<CalorieCalculationResponseDto>>> GetLatestCalculation()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new ApiResponse<CalorieCalculationResponseDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });

                var latestCalculation = await _context.CalorieCalculations
                    .Include(c => c.UserProfile)
                    .Where(c => c.UserId == userId.Value)
                    .OrderByDescending(c => c.CalculatedAt)
                    .FirstOrDefaultAsync();

                if (latestCalculation == null)
                {
                    return Ok(new ApiResponse<CalorieCalculationResponseDto>
                    {
                        Success = true,
                        Message = "No calculations found",
                        Data = null
                    });
                }

                var responseDto = MapToCalorieCalculationDto(latestCalculation);

                return Ok(new ApiResponse<CalorieCalculationResponseDto>
                {
                    Success = true,
                    Message = "Latest calculation retrieved successfully",
                    Data = responseDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<CalorieCalculationResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the latest calculation",
                    Errors = { ex.Message }
                });
            }
        }

        // GET: api/CalorieCalculator/history
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<CalorieHistoryDto>>>> GetCalculationHistory([FromQuery] int? limit = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new ApiResponse<List<CalorieHistoryDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });

                var calculations = await _context.CalorieCalculations
                    .Include(c => c.UserProfile)
                    .Where(c => c.UserId == userId.Value)
                    .OrderByDescending(c => c.CalculatedAt)
                    .Take(limit ?? 10)
                    .Select(c => new CalorieHistoryDto
                    {
                        Id = c.Id,
                        BMR = c.BMR,
                        MaintenanceCalories = c.MaintenanceCalories,
                        WeightLossCalories = c.WeightLossCalories,
                        WeightGainCalories = c.WeightGainCalories,
                        BMI = c.BMI,
                        Weight = c.UserProfile.Weight,
                        Goal = c.UserProfile.Goal,
                        CalculatedAt = c.CalculatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<CalorieHistoryDto>>
                {
                    Success = true,
                    Message = "History retrieved successfully",
                    Data = calculations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<CalorieHistoryDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving history",
                    Errors = { ex.Message }
                });
            }
        }

        // DELETE: api/CalorieCalculator/calculation/{id}
        [HttpDelete("calculation/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCalculation(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });

                var calculation = await _context.CalorieCalculations
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

                if (calculation == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Calculation not found"
                    });
                }

                _context.CalorieCalculations.Remove(calculation);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Calculation deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the calculation",
                    Errors = { ex.Message }
                });
            }
        }

        #region Private Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        private async Task<UserProfile> CreateOrUpdateUserProfile(int userId, UserProfileRequestDto request)
        {
            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProfile != null)
            {
                // Update existing profile
                existingProfile.Age = request.Age;
                existingProfile.Gender = request.Gender;
                existingProfile.Weight = request.Weight;
                existingProfile.Height = request.Height;
                existingProfile.ActivityLevel = request.ActivityLevel;
                existingProfile.BodyFatPercentage = request.BodyFatPercentage;
                existingProfile.Goal = request.Goal;
                existingProfile.UpdatedAt = DateTime.UtcNow;

                _context.UserProfiles.Update(existingProfile);
                await _context.SaveChangesAsync();

                return existingProfile;
            }
            else
            {
                // Create new profile
                var newProfile = new UserProfile
                {
                    UserId = userId,
                    Age = request.Age,
                    Gender = request.Gender,
                    Weight = request.Weight,
                    Height = request.Height,
                    ActivityLevel = request.ActivityLevel,
                    BodyFatPercentage = request.BodyFatPercentage,
                    Goal = request.Goal
                };

                _context.UserProfiles.Add(newProfile);
                await _context.SaveChangesAsync();

                return newProfile;
            }
        }

        private UserProfileResponseDto MapToUserProfileDto(UserProfile profile)
        {
            return new UserProfileResponseDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Age = profile.Age,
                Gender = profile.Gender,
                Weight = profile.Weight,
                Height = profile.Height,
                ActivityLevel = profile.ActivityLevel,
                BodyFatPercentage = profile.BodyFatPercentage,
                Goal = profile.Goal,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }

        private CalorieCalculationResponseDto MapToCalorieCalculationDto(CalorieCalculation calculation)
        {
            var macros = _calorieService.CalculateMacros(calculation.MaintenanceCalories, calculation.UserProfile.Goal);

            return new CalorieCalculationResponseDto
            {
                Id = calculation.Id,
                UserId = calculation.UserId,
                UserProfile = MapToUserProfileDto(calculation.UserProfile),
                BMR = calculation.BMR,
                MaintenanceCalories = calculation.MaintenanceCalories,
                WeightLossCalories = calculation.WeightLossCalories,
                WeightGainCalories = calculation.WeightGainCalories,
                BMI = calculation.BMI,
                BMICategory = _calorieService.GetBMICategory(calculation.BMI),
                IdealWeightMin = calculation.IdealWeightMin,
                IdealWeightMax = calculation.IdealWeightMax,
                Macros = macros,
                CalculatedAt = calculation.CalculatedAt
            };
        }

        #endregion
    }
}