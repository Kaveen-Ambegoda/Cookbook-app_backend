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
                    Name = recipeDomain.Name,
                    ImageUrl = recipeDomain.ImageUrl,
                    CookingTime = recipeDomain.CookingTime,
                    Servings = recipeDomain.Servings,
                });
            }

            return Ok(recipesDto);
        }


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
