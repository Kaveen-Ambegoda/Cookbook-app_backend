namespace CookbookApp.APi.Models.DTO
{
    public class GetRecipeDto
    {
        public int Id { get; set; } // Recipe ID
        public string Title { get; set; }
        public int CookingTime { get; set; }
        public int Portion { get; set; }
        public string ImageUrl { get; set; }
    }
}
