using Microsoft.EntityFrameworkCore;
using CookbookApp.APi.Models;

namespace CookbookApp.APi.Data
{
    public class CookbookDbContext : DbContext
    {
        public CookbookDbContext(DbContextOptions dbContextOptions): base(dbContextOptions) { }

        public DbSet<Recipe> Recipes { get; set; }

    }
}
