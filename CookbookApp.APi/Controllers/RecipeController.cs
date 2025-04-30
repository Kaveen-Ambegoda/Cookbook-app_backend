using CookbookApp.APi.Data;
using CookbookApp.APi.Models;

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
    public class RecipeController : Controller
    {
        private readonly CookbookDbContext _context;

        public RecipeController(CookbookDbContext dbContext) => _context = dbContext;

        [HttpPost("AddRecipe")]
        public async Task<IActionResult> AddRecipe([FromBody] Recipe recipe)
        {
            try
            {
                _context.Recipes.Add(recipe);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Ok(new { message = "Recipe added successfully", recipeId = recipe.Id });
                }
                return BadRequest(new { error = "Failed to add recipe" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

