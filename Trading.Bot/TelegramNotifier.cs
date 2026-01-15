using Telegram.Bot;
using Trading.ApplicationContracts.Services;

namespace Trading.Bot;

public class TelegramNotifier(string botToken) : INotifier
{
    private readonly ITelegramBotClient _bot = new TelegramBotClient(botToken);
    public long? ChatId { get; set; }

    public async Task Notify(string message)
    {
        if (ChatId.HasValue)
        {
            try
            {
                await _bot.SendMessage(ChatId.Value, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }
    }
}
