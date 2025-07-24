namespace CookbookApp.APi.Models.DTO
{
    public class AddReviewRequest
    {
        public int RecipeId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}
