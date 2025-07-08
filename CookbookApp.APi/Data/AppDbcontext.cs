using Microsoft.EntityFrameworkCore;
using CookbookAppBackend.Models;

namespace CookbookAppBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Existing entities
        public DbSet<User> Users { get; set; }

        // Community forum entities
        public DbSet<Forum> Forums { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<UserVote> UserVotes { get; set; } // Add UserVote entity

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Forum entity
            modelBuilder.Entity<Forum>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Url)
                    .HasMaxLength(500);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Comments)
                    .WithOne(c => c.Forum)
                    .HasForeignKey(c => c.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Favorites)
                    .WithOne(f => f.Forum)
                    .HasForeignKey(f => f.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure UserVotes relationship
                entity.HasMany(e => e.UserVotes)
                    .WithOne(v => v.Forum)
                    .HasForeignKey(v => v.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Comment entity
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete conflicts

                entity.HasOne(e => e.Forum)
                    .WithMany(f => f.Comments)
                    .HasForeignKey(e => e.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Replies)
                    .WithOne(r => r.Comment)
                    .HasForeignKey(r => r.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Reply entity
            modelBuilder.Entity<Reply>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete conflicts

                entity.HasOne(e => e.Comment)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(e => e.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Favorite entity
            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete conflicts

                entity.HasOne(e => e.Forum)
                    .WithMany(f => f.Favorites)
                    .HasForeignKey(e => e.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ensure unique constraint for user-forum combination
                entity.HasIndex(e => new { e.UserId, e.ForumId })
                    .IsUnique()
                    .HasDatabaseName("IX_Favorites_UserId_ForumId");
            });

            // Configure UserVote entity
            modelBuilder.Entity<UserVote>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.VoteType)
                    .HasConversion<int>() // Store enum as int
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete conflicts

                entity.HasOne(e => e.Forum)
                    .WithMany(f => f.UserVotes)
                    .HasForeignKey(e => e.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ensure unique constraint: one vote per user per forum
                entity.HasIndex(e => new { e.UserId, e.ForumId })
                    .IsUnique()
                    .HasDatabaseName("IX_UserVotes_UserId_ForumId");
            });

            // Configure indexes for better performance
            modelBuilder.Entity<Forum>()
                .HasIndex(e => e.Category)
                .HasDatabaseName("IX_Forums_Category");

            modelBuilder.Entity<Forum>()
                .HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_Forums_CreatedAt");

            modelBuilder.Entity<Forum>()
                .HasIndex(e => new { e.Upvotes, e.Downvotes })
                .HasDatabaseName("IX_Forums_Votes");

            modelBuilder.Entity<Comment>()
                .HasIndex(e => e.ForumId)
                .HasDatabaseName("IX_Comments_ForumId");

            modelBuilder.Entity<Reply>()
                .HasIndex(e => e.CommentId)
                .HasDatabaseName("IX_Replies_CommentId");

            modelBuilder.Entity<UserVote>()
                .HasIndex(e => e.ForumId)
                .HasDatabaseName("IX_UserVotes_ForumId");

            modelBuilder.Entity<UserVote>()
                .HasIndex(e => e.UserId)
                .HasDatabaseName("IX_UserVotes_UserId");
        }
    }
}