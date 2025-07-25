using Microsoft.EntityFrameworkCore;
using CookbookApp.APi.Models.Domain; // For Recipe
using CookbookAppBackend.Models; // For User
using CookbookApp.APi.Models;

namespace CookbookApp.APi.Data
{
    public class CookbookDbContext : DbContext
    {
        public CookbookDbContext(DbContextOptions<CookbookDbContext> options) : base(options) { }

        
        public DbSet<User> Users { get; set; }
        public DbSet<UserRestrictions> UserRestrictions { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Notification mapping
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");

                entity.HasKey(n => n.Id);

                entity.Property(n => n.Title)
                      .HasMaxLength(200)
                      .IsRequired();

                entity.Property(n => n.Description)
                      .HasMaxLength(1000)
                      .IsRequired();

                // Store enums as strings
                entity.Property(n => n.Type)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(n => n.Status)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(n => n.Priority)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(n => n.TargetType)
                      .HasConversion<string>()
                      .HasMaxLength(50);

                entity.Property(n => n.Category)
                      .HasMaxLength(100);

                entity.Property(n => n.ReporterName)
                      .HasMaxLength(150);

                entity.Property(n => n.TargetName)
                      .HasMaxLength(150);

                // JSON payload
                entity.Property(n => n.DetailsJson)
                      .HasColumnType("nvarchar(max)");

                // Indexes for filtering
                entity.HasIndex(n => n.Type);
                entity.HasIndex(n => n.Status);
                entity.HasIndex(n => n.Priority);
                entity.HasIndex(n => n.IsRead);
                entity.HasIndex(n => n.CreatedUtc);
            });
            {
                modelBuilder.Entity<Recipe>()
                    .HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserID);
            }
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("normal");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.RegisteredDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.LastActive).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.ReportCount).HasDefaultValue(0);
                entity.Property(e => e.Reported).HasDefaultValue(false);
                entity.Property(e => e.LiveVideos).HasDefaultValue(0);
                entity.Property(e => e.Posts).HasDefaultValue(0);
                entity.Property(e => e.Events).HasDefaultValue(0);
                entity.Property(e => e.Followers).HasDefaultValue(0);
                entity.Property(e => e.Likes).HasDefaultValue(0);
                entity.Property(e => e.Comments).HasDefaultValue(0);
                entity.Property(e => e.VideosWatched).HasDefaultValue(0);
                entity.Property(e => e.EngagementScore).HasDefaultValue(0);

                entity.Property(e => e.Avatar).HasMaxLength(500);
                entity.Property(e => e.SubscriptionType).HasMaxLength(20);
                entity.Property(e => e.ReportReason).HasMaxLength(500);
            });

            // Configure UserRestrictions entity
            modelBuilder.Entity<UserRestrictions>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Commenting).HasDefaultValue(false);
                entity.Property(e => e.Liking).HasDefaultValue(false);
                entity.Property(e => e.Posting).HasDefaultValue(false);
                entity.Property(e => e.Messaging).HasDefaultValue(false);
                entity.Property(e => e.LiveStreaming).HasDefaultValue(false);

                // Configure relationship
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Restrictions)
                      .HasForeignKey<UserRestrictions>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
        
    }
}
