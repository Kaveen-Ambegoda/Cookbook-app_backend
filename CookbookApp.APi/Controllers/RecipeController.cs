using CookbookApp.APi.Data;
using CookbookApp.APi.Models;
using CookbookApp.APi.Models.DTO;
using CookbookApp.APi.Services;
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
        private readonly ICloudinaryService _cloudinaryService;

        public RecipeController(CookbookDbContext dbContext, ICloudinaryService cloudinaryService)
        {
            _context = dbContext;
            _cloudinaryService = cloudinaryService;
        }


        //Save Recipe
        
        [HttpPost("AddRecipe")]
        public async Task<IActionResult> AddRecipe([FromBody] Recipe recipe)
        {
            try
            {
                _context.Recipes.Add(recipe);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Ok(new 
                    { message = "Recipe added successfully", recipeId = recipe.Id }
                    );
                }
                return BadRequest(new 
                { error = "Failed to add recipe" }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new 
                { error = ex.Message }
                );
            }
        }
        

        //Get all recipes

        [HttpGet("recipeManagePage")]
        public IActionResult GetAllRecipes()
        {
            // Get data from the database
            var recipesDomain = _context.Recipes.ToList();

            // Map domain models to DTO
            var getRecipesDto = new List<GetRecipeDto>();

            foreach (var recipeDomain in recipesDomain)
            {
                getRecipesDto.Add(new GetRecipeDto()
                {
                    Id = recipeDomain.Id,
                    Title = recipeDomain.Title,
                    Image = recipeDomain.Image,
                });
            }

            return Ok(getRecipesDto);
        }

        //Get a single Recipe
        [HttpGet("{id}")]
        public IActionResult GetRecipeById(int id)
        {
            var recipeDomain = _context.Recipes.FirstOrDefault(x => x.Id == id);
            if (recipeDomain == null)
                return NotFound();

            //Map Recipe Domain to DTO

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
            //return DTO back 
            return Ok(recipeDto);
        }

        //Delete recipe by ID

        [HttpDelete("deleteRecipe/{id}")]
        public async Task<IActionResult> deleteProduct(int id)
        {
            try
            {
                var ExistingRecipe = await _context.Recipes.FindAsync(id);
                if (ExistingRecipe == null)
                {
                    return NotFound(new
                    { error = "Recipe not found"
                    });
                }
                _context.Recipes.Remove(ExistingRecipe);
                int r = await _context.SaveChangesAsync();
                if (r > 0) {
                    return Ok(new 
                    { 
                        message = "Recipe deleted successfully" 
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        error = "Failed to delete recipe"
                    });
                }

            }
            catch (Exception ex)
            {
                return BadRequest(new
                { error = ex.Message
                });
            }
        }

        /*

        //Update Recipe
        [HttpPut("updateRecipe/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> updateRecipe(int id, [FromForm] AllRecipeDto updatedRecipe)
        {
            try
            {
                var existingRecipe = await _context.Recipes.FindAsync(id);

                if (existingRecipe == null)
                {
                    return NotFound(new
                    {
                        Message = "No Recipe Found"
                    });
                }

                existingRecipe.Title = updatedRecipe.Title;
                existingRecipe.Image = updatedRecipe.Image;
                existingRecipe.Portion = updatedRecipe.Portion;
                existingRecipe.CookingTime = updatedRecipe.CookingTime;
                existingRecipe.Category = updatedRecipe.Category;
                existingRecipe.Calories = updatedRecipe.Calories;
                existingRecipe.Carbs = updatedRecipe.Carbs;
                existingRecipe.Fat = updatedRecipe.Fat;
                existingRecipe.Protein = updatedRecipe.Protein;
                existingRecipe.Ingredients = updatedRecipe.Ingredients;
                existingRecipe.Instructions = updatedRecipe.Instructions;

                _context.Recipes.Update(existingRecipe);

                int r = await _context.SaveChangesAsync();
                if (r > 0)
                {
                    return Ok(new
                    {
                        Message = "Product Updated"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Error = "Cannot Update"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message
                });
            }
        }

        */

        //Get All Recipies to HomePage


        [HttpGet("homePage")]
        public IActionResult GetHomePageRecipes()
        {
            // Get data from the database
            var recipesDomain = _context.Recipes.ToList();

            // Map domain models to DTO
            var homeRecipeDto = new List<GetHomeRecipeDto>();

            foreach (var recipeDomain in recipesDomain)
            {
                homeRecipeDto.Add(new GetHomeRecipeDto()
                {
                    Id = recipeDomain.Id,
                    Title = recipeDomain.Title,
                    Image = recipeDomain.Image,
                    CookingTime = recipeDomain.CookingTime,
                    Portion = recipeDomain.Portion,

                });
            }

            return Ok(homeRecipeDto);
        }

        

        [HttpPost("AddRecipe1")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddRecipe1([FromForm] AllRecipeDto dto)
        {
            try
            {
                string imageUrl = null;

                if (dto.Image?.Length > 0)
                {
                    // Upload image to Cloudinary
                    imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image);
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
                    Image = imageUrl
                };

                _context.Recipes.Add(recipe);
                if (await _context.SaveChangesAsync() > 0)
                    return Ok(new { message = "Recipe added successfully", recipeId = recipe.Id });

                return BadRequest(new { error = "Failed to add recipe" });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = errorMessage });
            }
        }

        //Update Recipe New


        [HttpPut("updateRecipe/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> updateRecipe(int id, [FromForm] AllRecipeDto updatedRecipe)
        {
            try
            {
                var existingRecipe = await _context.Recipes.FindAsync(id);

                if (existingRecipe == null)
                {
                    return NotFound(new
                    {
                        Message = "No Recipe Found"
                    });
                }

                // Handle image upload if a new image is provided
                if (updatedRecipe.Image?.Length > 0)
                {
                    // Upload new image to Cloudinary
                    string imageUrl = await _cloudinaryService.UploadImageAsync(updatedRecipe.Image);
                    existingRecipe.Image = imageUrl;
                }

                // Update the rest of the fields
                existingRecipe.Title = updatedRecipe.Title;
                existingRecipe.Portion = updatedRecipe.Portion;
                existingRecipe.CookingTime = updatedRecipe.CookingTime;
                existingRecipe.Category = updatedRecipe.Category;
                existingRecipe.Calories = updatedRecipe.Calories;
                existingRecipe.Carbs = updatedRecipe.Carbs;
                existingRecipe.Fat = updatedRecipe.Fat;
                existingRecipe.Protein = updatedRecipe.Protein;
                existingRecipe.Ingredients = updatedRecipe.Ingredients;
                existingRecipe.Instructions = updatedRecipe.Instructions;

                _context.Recipes.Update(existingRecipe);

                if (await _context.SaveChangesAsync() > 0)
                {
                    return Ok(new
                    {
                        Message = "Recipe Updated Successfully",
                        RecipeId = existingRecipe.Id
                    });
                }

                return BadRequest(new
                {
                    Error = "Failed to update recipe"
                });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { Error = errorMessage });
            }
        }



    }
}
