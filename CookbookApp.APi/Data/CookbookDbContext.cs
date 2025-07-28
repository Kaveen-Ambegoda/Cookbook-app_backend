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
        public DbSet<Review> Reviews { get; set; }
        public DbSet<FavoriteRecipe> FavoriteRecipes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Recipe → User (with optional cascading if needed)
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.Cascade); // Or Restrict if needed

            // FavoriteRecipe → Recipe
            modelBuilder.Entity<FavoriteRecipe>()
                .HasOne(fr => fr.Recipe)
                .WithMany()
                .HasForeignKey(fr => fr.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);  // This can safely be cascade

            // FavoriteRecipe → User (conflicting path, set to Restrict to avoid error)
            modelBuilder.Entity<FavoriteRecipe>()
                .HasOne(fr => fr.User)
                .WithMany()
                .HasForeignKey(fr => fr.UserId)
                .OnDelete(DeleteBehavior.Restrict); // This is the key to fix the migration error

            // Review → Recipe (can keep cascade)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Recipe)
                .WithMany(r => r.Reviews)
                .HasForeignKey(r => r.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review → User (FIXED)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            //By default all users are active
            modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasDefaultValue("active");

        }


    }
}
