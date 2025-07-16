namespace CookbookApp.APi.Models.DTO
{
    public class GetHomeRecipeDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int CookingTime { get; set; }
        public int Portion { get; set; }
        public string Image { get; set; }
    }
}
