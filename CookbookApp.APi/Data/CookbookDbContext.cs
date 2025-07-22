using Microsoft.EntityFrameworkCore;
using CookbookApp.APi.Models.Domain;
using CookbookAppBackend.Models;

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
        public DbSet<FavoriteRecipe> FavoriteRecipes { get; set; }
        public DbSet<ForumVote> ForumVotes { get; set; } // New DbSet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Forum Feature Relationships ---

            // Forum -> User (Author)
            modelBuilder.Entity<Forum>()
                .HasOne(f => f.Author)
                .WithMany()
                .HasForeignKey(f => f.AuthorId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete user if their forum is deleted

            // Comment -> Forum (Cascade delete comments when a forum is deleted)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Forum)
                .WithMany(f => f.Comments)
                .HasForeignKey(c => c.ForumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment -> User
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reply -> Comment (Cascade delete replies when a comment is deleted)
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Comment)
                .WithMany(c => c.Replies)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Reply -> User
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserFavorite -> Forum
            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.Forum)
                .WithMany(f => f.UserFavorites)
                .HasForeignKey(uf => uf.ForumId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserFavorite -> User
            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithMany()
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ForumVote -> Forum
            modelBuilder.Entity<ForumVote>()
                .HasOne(v => v.Forum)
                .WithMany(f => f.Votes)
                .HasForeignKey(v => v.ForumId)
                .OnDelete(DeleteBehavior.Cascade);

            // ForumVote -> User
            modelBuilder.Entity<ForumVote>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // --- Recipe Feature Relationships (from your original code) ---
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoriteRecipe>()
                .HasOne(fr => fr.Recipe)
                .WithMany()
                .HasForeignKey(fr => fr.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoriteRecipe>()
                .HasOne(fr => fr.User)
                .WithMany()
                .HasForeignKey(fr => fr.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}