using Microsoft.EntityFrameworkCore;
using CookbookApp.APi.Models.Domain; // For Recipe
using CookbookApp.APi.Models.Domain; // For Forum, Comment, Reply, UserFavorite
using CookbookAppBackend.Models;     // For User

namespace CookbookApp.APi.Data
{
    public class CookbookDbContext : DbContext
    {
        public CookbookDbContext(DbContextOptions<CookbookDbContext> options) : base(options) { }

        
        public DbSet<User> Users { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserID);
        }
    }
}
