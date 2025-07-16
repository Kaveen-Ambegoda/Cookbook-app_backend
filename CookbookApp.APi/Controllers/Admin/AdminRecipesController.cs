using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CookbookApp.APi.Data;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO.Admin.Recipe;
namespace CookbookApp.APi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class AdminRecipesController : ControllerBase
    {
        private readonly CookbookDbContext _context;

        public AdminRecipesController(CookbookDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/admin/recipes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetRecipeDto>>> GetAllRecipes()
        {
            var recipes = await _context.Recipes.ToListAsync();

            var result = recipes.Select(r => new GetRecipeDto
            {
                Id = r.Id,
                Title = r.Title,
                Category = r.Category,
                CookingTime = r.CookingTime,
                Portion = r.Portion,
                Calories = r.Calories,
                Carbs = r.Carbs,
                Protein = r.Protein,
                Fat = r.Fat,
                Ingredients = r.Ingredients,
                Instructions = r.Instructions,
                Image = r.Image
            }).ToList();

            return Ok(result);
        }

        // ✅ GET: api/admin/recipes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GetRecipeDto>> GetRecipeById(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound();

            var dto = new GetRecipeDto
            {
                Id = recipe.Id,
                Title = recipe.Title,
                Category = recipe.Category,
                CookingTime = recipe.CookingTime,
                Portion = recipe.Portion,
                Calories = recipe.Calories,
                Carbs = recipe.Carbs,
                Protein = recipe.Protein,
                Fat = recipe.Fat,
                Ingredients = recipe.Ingredients,
                Instructions = recipe.Instructions,
                Image = recipe.Image
            };

            return Ok(dto);
        }

        // ✅ POST: api/admin/recipes
        [HttpPost]
        public async Task<ActionResult> CreateRecipe([FromBody] RecipeDto recipeDto)
        {
            var recipe = new Recipe
            {
                Title = recipeDto.Title,
                Category = recipeDto.Category,
                CookingTime = recipeDto.CookingTime,
                Portion = recipeDto.Portion,
                Calories = recipeDto.Calories,
                Carbs = recipeDto.Carbs,
                Protein = recipeDto.Protein,
                Fat = recipeDto.Fat,
                Ingredients = recipeDto.Ingredients,
                Instructions = recipeDto.Instructions,
                Image = recipeDto.Image,
                UserID = recipeDto.UserID // assume admin knows which user owns the recipe
            };

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecipeById), new { id = recipe.Id }, recipe);
        }

        // ✅ PUT: api/admin/recipes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromBody] RecipeDto recipeDto)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound();

            recipe.Title = recipeDto.Title;
            recipe.Category = recipeDto.Category;
            recipe.CookingTime = recipeDto.CookingTime;
            recipe.Portion = recipeDto.Portion;
            recipe.Calories = recipeDto.Calories;
            recipe.Carbs = recipeDto.Carbs;
            recipe.Protein = recipeDto.Protein;
            recipe.Fat = recipeDto.Fat;
            recipe.Ingredients = recipeDto.Ingredients;
            recipe.Instructions = recipeDto.Instructions;
            recipe.Image = recipeDto.Image;
            recipe.UserID = recipeDto.UserID;

            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ DELETE: api/admin/recipes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound();

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
