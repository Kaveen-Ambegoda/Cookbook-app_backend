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

        public RecipeController(CookbookDbContext dbContext)
        {
            this._context = dbContext;
        }

        
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

        //Get all recipes

        [HttpGet]
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
                    ImageUrl = recipeDomain.ImageUrl,
                    CookingTime = recipeDomain.CookingTime,
                    Portion = recipeDomain.Portion,

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
                ImageUrl = recipeDomain.ImageUrl,
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
                    return Ok(new { message = "Recipe deleted successfully" });
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

        //Update Recipe
        [HttpPut("updateRecipe/{id}")]
        public async Task<IActionResult> updateRecipe(int id, [FromBody] UpdateRecipeDto updatedRecipe)
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
                existingRecipe.ImageUrl = updatedRecipe.ImageUrl;
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


        /*
        
        //GET all recipes
        [HttpGet]
        public IActionResult GetAllRecipes()
        {
            // Get data from the database
            var recipesDomain = _context.Recipes.ToList();

            // Map domain models to DTO
            var recipesDto = new List<RecipeDto>();

            foreach (var recipeDomain in recipesDomain)
            {
                recipesDto.Add(new RecipeDto()
                {
                    Id = recipeDomain.Id,
                    Title = recipeDomain.Title,
                    ImageUrl = recipeDomain.ImageUrl,
                    CookingTime = recipeDomain.CookingTime,
                    Portion = recipeDomain.Portion,
                });
            }

            return Ok(recipesDto);
        }
        */


        /*

        //GET single recipe by ID
        [HttpGet("{id}")]
        public IActionResult GetRecipeById(int id)
        {
            var recipeDomain = _context.Recipes.FirstOrDefault(x => x.Id==id);
            if (recipeDomain == null)
                return NotFound();

            //Map Recipe Domain to DTO

            var recipeDto = new RecipeDto
            {
                Id = recipeDomain.Id,
                Name = recipeDomain.Name,
                ImageUrl = recipeDomain.ImageUrl,
                CookingTime = recipeDomain.CookingTime,
                Servings = recipeDomain.Servings,
            };
            //return DTO back 
            return Ok(recipeDto);
        }
        [HttpPost]
        public IActionResult Create([FromBody] AddRecipeRequestDto addRecipeRequestDto)
        {
            //Map Dto to Domain
            var recipeDomainModel = new Recipe
            {
                Name = addRecipeRequestDto.Name,
                ImageUrl = addRecipeRequestDto.ImageUrl,
                CookingTime = addRecipeRequestDto.CookingTime,
                Servings = addRecipeRequestDto.Servings,
            };

            //Use Domain Model to create Recipe
            _context.Recipes.Add(recipeDomainModel);
            _context.SaveChanges();
            //Map Domain model back to Dto
            var recipeDto = new RecipeDto
            {
                Id = recipeDomainModel.Id,
                Name = recipeDomainModel.Name,
                ImageUrl = recipeDomainModel.ImageUrl,
                CookingTime = recipeDomainModel.CookingTime,
                Servings = recipeDomainModel.Servings,
            };

            return CreatedAtAction(nameof(GetRecipeById), new {id=recipeDomainModel.Id}, recipeDomainModel);
        }
        */

        [HttpGet("searchRecipe/{id}")]
        public async Task<IActionResult> SearchRecipe(int id)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);

                if (recipe == null)
                    return NotFound(); // Handle if no recipe is found

                return Ok(recipe); // Return recipe if found
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message
                });
            }
        }


    }
}
