// src/Models/Domain/ForumVote.cs

using CookbookAppBackend.Models;
using System.ComponentModel.DataAnnotations;

namespace CookbookApp.APi.Models.Domain
{
    public class ForumVote
    {
        public Guid Id { get; set; }

        [Required]
        public Guid ForumId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 1 for Upvote, -1 for Downvote
        /// </summary>
        [Required]
        public int VoteType { get; set; }

        // Navigation properties
        public Forum Forum { get; set; }
        public User User { get; set; }
    }
}