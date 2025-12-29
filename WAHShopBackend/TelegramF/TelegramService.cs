namespace WAHShopBackend.TelegramF
{
    using Microsoft.Extensions.Options;
    using Telegram.Bot;
    using Telegram.Bot.Types;
    using WAHShopBackend.Models;

    public class TelegramService(IOptions<TelegramBotSettings> TelegramBotSettings, TelegramBotClient telegramBotClient)
    {
        private readonly IOptions<TelegramBotSettings> _settings = TelegramBotSettings;
        private readonly TelegramBotClient _telegramBotClient = telegramBotClient;

        public async Task SendMessageAsync(string message)
        {
            try
            {
                await _telegramBotClient.SendMessage(
                    chatId: _settings.Value.ChatId,
                    text: message
                );
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error sending message to Telegram: {ex.Message}");
            }
        }
    }
}
