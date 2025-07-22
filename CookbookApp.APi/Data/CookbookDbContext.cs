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
        }
        
    }
}
