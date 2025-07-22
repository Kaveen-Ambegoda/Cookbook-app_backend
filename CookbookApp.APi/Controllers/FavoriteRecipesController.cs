using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CookbookApp.APi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FavoriteRecipesController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        public FavoriteRecipesController(CookbookDbContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody] int recipeId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var existing = await _context.FavoriteRecipes
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RecipeId == recipeId);

            if (existing != null)
                return BadRequest("Recipe already in favorites.");

            var recipe = await _context.Recipes.FindAsync(recipeId);
            if (recipe == null)
                return NotFound("Recipe not found.");

            var favorite = new FavoriteRecipe
            {
                UserId = userId,
                RecipeId = recipeId
            };

            _context.FavoriteRecipes.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var favorites = await _context.FavoriteRecipes
                .Where(f => f.UserId == userId)
                .Include(f => f.Recipe)
                .Select(f => new
                {
                    f.Recipe.Id,
                    f.Recipe.Title,
                    f.Recipe.Image,
                    f.Recipe.CookingTime,
                    f.Recipe.Portion,  
                    
                })
                .ToListAsync();

            return Ok(favorites);
        }

        //Remove from favorites
        [Authorize]
        [HttpDelete("{recipeId}")]
        public async Task<IActionResult> RemoveFavorite(int recipeId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var favorite = await _context.FavoriteRecipes
                .FirstOrDefaultAsync(f => f.RecipeId == recipeId && f.UserId == userId);

            if (favorite == null)
                return NotFound("Favorite not found");

            _context.FavoriteRecipes.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok("Favorite removed");
        }


        //count Favorite Recipe

        [AllowAnonymous]
        [HttpGet("count/{recipeId}")]
        public async Task<IActionResult> GetFavoriteCount(int recipeId)
        {
            var count = await _context.FavoriteRecipes
                .Where(f => f.RecipeId == recipeId)
                .CountAsync();

            return Ok(count);
        }

        //IsFavorite list
        [Authorize]
        [HttpGet("is-favorited/{recipeId}")]
        public async Task<IActionResult> IsFavorited(int recipeId)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            var isFav = await _context.FavoriteRecipes
                .AnyAsync(f => f.RecipeId == recipeId && f.UserId == userId);

            return Ok(isFav);
        }

    }
}
