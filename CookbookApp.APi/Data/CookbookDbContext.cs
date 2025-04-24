using Microsoft.EntityFrameworkCore;
using CookbookApp.APi.Models;
using CookbookApp.API.Models.Domain;

namespace CookbookApp.APi.Data
{
    public class CookbookDbContext : DbContext
    {
        public CookbookDbContext(DbContextOptions<CookbookDbContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<Recipe> Recipes { get; set; }
    }
}
