// Models/DTO/VoteResponseDto.cs

namespace CookbookApp.APi.Models.DTO
{
    public class VoteResponseDto
    {
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public string? UserVote { get; set; } // "upvote", "downvote", or null
    }
}