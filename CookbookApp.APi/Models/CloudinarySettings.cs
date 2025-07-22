// Location: Models/CloudinarySettings.cs

namespace CookbookApp.APi.Models
{
    public class CloudinarySettings
    {
        // Initialized to prevent CS8618 warnings during build
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
    }
}