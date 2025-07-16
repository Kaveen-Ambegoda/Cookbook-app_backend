using Microsoft.EntityFrameworkCore;
using CookbookApp.APi.Models;
using CookbookApp.API.Models.Domain;
using CookbookApp.APi.Models.Domain;
using CookbookApp.APi.Models.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace CookbookApp.APi.Data
{
    public class CookbookDbContext : DbContext
    {
        public CookbookDbContext(DbContextOptions<CookbookDbContext> dbContextOptions) : base(dbContextOptions) { }

        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionVote> SubmissionVotes { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Rating> Ratings { get; set; }
    }
}
