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

        // GET: api/admin/adminrecipes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetRecipeDto>>> GetAllRecipes()
        {
            try
            {
                var recipes = await _context.Recipes
                    .Include(r => r.User)
                    .ToListAsync();

                var result = recipes.Select(r => MapToGetRecipeDto(r)).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving recipes: {ex.Message}");
            }
        }

        // GET: api/admin/adminrecipes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GetRecipeDto>> GetRecipeById(int id)
        {
            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (recipe == null)
                    return NotFound($"Recipe with ID {id} not found.");

                var dto = MapToGetRecipeDto(recipe);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving recipe: {ex.Message}");
            }
        }

        // POST: api/admin/adminrecipes
        [HttpPost]
        public async Task<ActionResult<GetRecipeDto>> CreateRecipe([FromBody] RecipeDto recipeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var recipe = MapFromRecipeDto(recipeDto);

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                // Return the created recipe as GetRecipeDto
                var createdRecipe = await _context.Recipes
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == recipe.Id);

                var resultDto = MapToGetRecipeDto(createdRecipe);
                return CreatedAtAction(nameof(GetRecipeById), new { id = recipe.Id }, resultDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating recipe: {ex.Message}");
            }
        }

        // PUT: api/admin/adminrecipes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromBody] RecipeDto recipeDto)
        {
            if (id != recipeDto.Id)
                return BadRequest("Recipe ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                    return NotFound($"Recipe with ID {id} not found.");

                UpdateRecipeFromDto(recipe, recipeDto);

                _context.Recipes.Update(recipe);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating recipe: {ex.Message}");
            }
        }

        // PUT: api/admin/adminrecipes/5/toggle-visibility
        [HttpPut("{id}/toggle-visibility")]
        public async Task<IActionResult> ToggleVisibility(int id, [FromBody] ToggleVisibilityDto? toggleDto = null)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                    return NotFound($"Recipe with ID {id} not found.");

                // Use provided value or toggle current value
                if (toggleDto != null && toggleDto.Visible.HasValue)
                {
                    recipe.Visible = toggleDto.Visible.Value;
                }
                else
                {
                    // Toggle current visibility
                    recipe.Visible = !recipe.Visible;
                }

                _context.Recipes.Update(recipe);
                await _context.SaveChangesAsync();

                return Ok(new { visible = recipe.Visible });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error toggling visibility: {ex.Message}");
            }
        }

        // PUT: api/admin/adminrecipes/5/visibility
        [HttpPut("{id}/visibility")]
        public async Task<IActionResult> SetVisibility(int id, [FromBody] ToggleVisibilityDto visibilityDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                    return NotFound($"Recipe with ID {id} not found.");

                recipe.Visible = visibilityDto.Visible.Value;

                _context.Recipes.Update(recipe);
                await _context.SaveChangesAsync();

                return Ok(new { visible = visibilityDto.Visible });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error setting visibility: {ex.Message}");
            }
        }

        // DELETE: api/admin/adminrecipes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                    return NotFound($"Recipe with ID {id} not found.");

                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting recipe: {ex.Message}");
            }
        }

        // GET: api/admin/adminrecipes/visible
        [HttpGet("visible")]
        public async Task<ActionResult<IEnumerable<GetRecipeDto>>> GetVisibleRecipes()
        {
            try
            {
                var recipes = await _context.Recipes
                    .Include(r => r.User)
                    .Where(r => r.Visible) // Direct property access
                    .ToListAsync();

                var result = recipes.Select(r => MapToGetRecipeDto(r)).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving visible recipes: {ex.Message}");
            }
        }

        // GET: api/admin/adminrecipes/category/{category}
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<GetRecipeDto>>> GetRecipesByCategory(string category)
        {
            try
            {
                var recipes = await _context.Recipes
                    .Include(r => r.User)
                    .Where(r => r.Category.ToLower() == category.ToLower())
                    .ToListAsync();

                var result = recipes.Select(r => MapToGetRecipeDto(r)).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving recipes by category: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private GetRecipeDto MapToGetRecipeDto(Recipe recipe)
        {
            return new GetRecipeDto
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
                Image = recipe.Image,
                UserID = recipe.UserID,
                Author = GetAuthorName(recipe),
                Description = !string.IsNullOrEmpty(recipe.Description)
                    ? recipe.Description
                    : GenerateDescriptionFromInstructions(recipe.Instructions),
                Visible = recipe.Visible
            };
        }

        private Recipe MapFromRecipeDto(RecipeDto dto)
        {
            return new Recipe
            {
                Title = dto.Title,
                Category = dto.Category,
                CookingTime = dto.CookingTime,
                Portion = dto.Portion,
                Calories = dto.Calories,
                Carbs = dto.Carbs,
                Protein = dto.Protein,
                Fat = dto.Fat,
                Ingredients = dto.Ingredients,
                Instructions = dto.Instructions,
                Image = dto.Image,
                UserID = dto.UserID,
                Description = dto.Description,
                Visible = dto.Visible
            };
        }

        private void UpdateRecipeFromDto(Recipe recipe, RecipeDto dto)
        {
            recipe.Title = dto.Title;
            recipe.Category = dto.Category;
            recipe.CookingTime = dto.CookingTime;
            recipe.Portion = dto.Portion;
            recipe.Calories = dto.Calories;
            recipe.Carbs = dto.Carbs;
            recipe.Protein = dto.Protein;
            recipe.Fat = dto.Fat;
            recipe.Ingredients = dto.Ingredients;
            recipe.Instructions = dto.Instructions;
            recipe.Image = dto.Image;
            recipe.UserID = dto.UserID;
            recipe.Description = dto.Description;
            recipe.Visible = dto.Visible;
        }

        private string GetAuthorName(Recipe recipe)
        {
            if (recipe.User != null && !string.IsNullOrEmpty(recipe.User.Username))
            {
                return recipe.User.Username;
            }
            return $"User {recipe.UserID}";
        }

        private string GenerateDescriptionFromInstructions(string instructions)
        {
            if (string.IsNullOrEmpty(instructions))
                return "";

            const int maxLength = 100;
            if (instructions.Length <= maxLength)
                return instructions;

            var truncated = instructions.Substring(0, maxLength);
            var lastSpace = truncated.LastIndexOf(' ');

            return lastSpace > 0 ? truncated.Substring(0, lastSpace) + "..." : truncated + "...";
        }

        #endregion
    }
}