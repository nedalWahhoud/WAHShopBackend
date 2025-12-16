namespace WAHShopBackend.Models
{
    public class AppConfig
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public Uri ApiUri => new(ApiBaseUrl);
        public string Domin { get; set; } = string.Empty;
        public string ImagesProxy { get; set; } = string.Empty;
        public string WebRequestProductImagePath { get; set; } = string.Empty;

        public string ShareStoragePath { get; set; } = string.Empty;
    }
}
