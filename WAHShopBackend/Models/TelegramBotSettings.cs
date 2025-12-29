namespace WAHShopBackend.Models
{
    public class TelegramBotSettings
    {
        public string Token { get; set; } = string.Empty;
        public long ChatId { get; set; }
    }
}
