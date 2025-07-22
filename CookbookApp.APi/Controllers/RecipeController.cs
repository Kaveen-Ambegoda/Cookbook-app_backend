// Location: Controllers/RecipeController.cs

using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using CookbookApp.APi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net; // Required for HttpStatusCode

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipeController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public RecipeController(CookbookDbContext dbContext, ICloudinaryService cloudinaryService)
        {
            _context = dbContext;
            _cloudinaryService = cloudinaryService;
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : 0;
        }

        [HttpGet("homePage")]
        public async Task<IActionResult> GetHomePageRecipes()
        {
            var homeRecipeDto = await _context.Recipes
                .Select(r => new GetHomeRecipeDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Image = r.Image,
                    CookingTime = r.CookingTime,
                    Portion = r.Portion,
                })
                .ToListAsync();

            return Ok(homeRecipeDto);
        }

        [Authorize]
        [HttpGet("myRecipes")]
        public async Task<IActionResult> GetMyRecipes()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var recipes = await _context.Recipes
                .Where(r => r.UserID == userId)
                .Select(r => new GetRecipeDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Image = r.Image
                })
                .ToListAsync();

            return Ok(recipes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipeById(int id)
        {
            var recipeDomain = await _context.Recipes.FindAsync(id);
            if (recipeDomain == null) return NotFound();

            var recipeDto = new RecipeDto
            {
                Id = recipeDomain.Id,
                Title = recipeDomain.Title,
                Image = recipeDomain.Image,
                CookingTime = recipeDomain.CookingTime,
                Portion = recipeDomain.Portion,
                Protein = recipeDomain.Protein,
                Calories = recipeDomain.Calories,
                Carbs = recipeDomain.Carbs,
                Fat = recipeDomain.Fat,
                Category = recipeDomain.Category,
                Ingredients = recipeDomain.Ingredients,
                Instructions = recipeDomain.Instructions
            };

            return Ok(recipeDto);
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateRecipe([FromForm] AddRecipeRequestDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            string? imageUrl = null;

            if (dto.Image?.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.Image);
                if (uploadResult.Error != null || uploadResult.StatusCode != HttpStatusCode.OK)
                {
                    return BadRequest(new { error = $"Image upload failed: {uploadResult.Error.Message}" });
                }
                imageUrl = uploadResult.SecureUrl.ToString();
            }

            var recipe = new Recipe
            {
                Title = dto.Title,
                Ingredients = dto.Ingredients,
                Instructions = dto.Instructions,
                Category = dto.Category,
                CookingTime = dto.CookingTime,
                Portion = dto.Portion,
                Calories = dto.Calories,
                Protein = dto.Protein,
                Fat = dto.Fat,
                Carbs = dto.Carbs,
                Image = imageUrl,
                UserID = userId
            };

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecipeById), new { id = recipe.Id }, recipe);
        }

        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromForm] UpdateRecipeDto updatedRecipeDto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existingRecipe = await _context.Recipes.FindAsync(id);
            if (existingRecipe == null)
            {
                return NotFound(new { message = "Recipe not found." });
            }

            if (existingRecipe.UserID != userId)
            {
                return Forbid("You are not authorized to update this recipe.");
            }

            if (updatedRecipeDto.Image?.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(updatedRecipeDto.Image);
                if (uploadResult.Error != null || uploadResult.StatusCode != HttpStatusCode.OK)
                {
                    return BadRequest(new { error = $"Image upload failed: {uploadResult.Error.Message}" });
                }
                existingRecipe.Image = uploadResult.SecureUrl.ToString();
            }

            existingRecipe.Title = updatedRecipeDto.Title;
            existingRecipe.Portion = updatedRecipeDto.Portion;
            existingRecipe.CookingTime = updatedRecipeDto.CookingTime;
            existingRecipe.Category = updatedRecipeDto.Category;
            existingRecipe.Calories = updatedRecipeDto.Calories;
            existingRecipe.Carbs = updatedRecipeDto.Carbs;
            existingRecipe.Fat = updatedRecipeDto.Fat;
            existingRecipe.Protein = updatedRecipeDto.Protein;
            existingRecipe.Ingredients = updatedRecipeDto.Ingredients;
            existingRecipe.Instructions = updatedRecipeDto.Instructions;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Recipe updated successfully." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound(new { error = "Recipe not found." });

            if (recipe.UserID != userId)
                return Forbid("You are not authorized to delete this recipe.");

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Recipe deleted successfully." });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchRecipes([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { error = "Keyword is required." });

            var lowerKeyword = keyword.ToLower();

            var matchedRecipes = await _context.Recipes
                .Where(r =>
                    r.Title.ToLower().Contains(lowerKeyword) ||
                    r.Ingredients.ToLower().Contains(lowerKeyword) ||
                    r.Instructions.ToLower().Contains(lowerKeyword)
                )
                .Select(r => new GetRecipeDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Image = r.Image
                })
                .ToListAsync();

            return Ok(matchedRecipes);
        }
    }
}