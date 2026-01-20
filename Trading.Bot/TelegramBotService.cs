using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Trading.ApplicationContracts.Services;

namespace Trading.Bot;

public class TelegramBotService(
    string botToken,
    IServiceProvider serviceProvider,
    TelegramNotifier notifier)
{
    private readonly ITelegramBotClient _bot = new TelegramBotClient(botToken);

    public void Start()
    {
        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync);

        Console.WriteLine("Telegram bot started");
    }

    static InlineKeyboardMarkup MainMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Get Open Positions", "get_open_positions"),
                InlineKeyboardButton.WithCallbackData("Start Trading", "start_trading"),
            }
            // new[]
            // {
            //     InlineKeyboardButton.WithCallbackData("💰 Balance", "balance")
            // }
        });
    }
    
    

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var positionService = scope.ServiceProvider.GetRequiredService<IActivePositionService>();

        try
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                var cb = update.CallbackQuery!;
                switch (cb.Data)
                {
                    case "get_open_positions":
                        var openPositions = await positionService.GetOpenPositions();
                        if (openPositions.Count == 0)
                        {
                            await bot.SendMessage(cb.Message.Chat.Id, "No open positions.", cancellationToken: ct,
                                replyMarkup: MainMenu());
                        }
                        else
                        {
                            
                            var response = "Open Positions:\n" + string.Join("\n", openPositions.Select(p =>
                                $"- {p.Key}\n" +
                                $"    Qty {p.Value.Quantity:F4},\n" +
                                $"    Avg Price {p.Value.AverageNetPrice:F2},\n" +
                                $"    Cost {p.Value.Cost:F2} {p.Value.SourceSymbol},\n" +
                                $"    CurrentPrice {p.Value.CurrentPrice},\n" +
                                $"    DipBuyPrice {p.Value.DipBuyPrice},\n" +
                                $"    ShortSellPrice {p.Value.ShortSellPrice},\n" +
                                $"    FinalSellPrice {p.Value.FinalSellPrice}"));
                            await bot.SendMessage(cb.Message.Chat.Id, response, cancellationToken: ct,
                                replyMarkup: MainMenu());
                        }

                        break;
                    case "start_trading":
                        var tradingService = scope.ServiceProvider.GetRequiredService<IActivePositionTradingService>();
                        var started = await tradingService.StartTrading();
                        if (started)
                        {
                            await bot.SendMessage(
                                cb.Message.Chat.Id,
                                "🚀 Trading started!",
                                cancellationToken: ct,
                                replyMarkup: MainMenu());
                        }
                        else
                        {
                            await bot.SendMessage(
                                cb.Message.Chat.Id,
                                "❌ Failed to start trading. Check logs for details.",
                                cancellationToken: ct,
                                replyMarkup: MainMenu());
                        }

                        break;
                }
            }

            else if (update.Type == UpdateType.Message)
            {

                if (update.Message.From.Id != 8023975240)
                {
                    await bot.SendMessage(
                        update.Message.Chat.Id,
                        "❌ You are not authorized for this chat.",
                        cancellationToken: ct);
                    return;
                }

                if (update.Message?.Text == null)
                    return;

                notifier.ChatId = update.Message.Chat.Id;

                var parts = update.Message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLowerInvariant();

                switch (command)
                {
                    case "/start":
                        await bot.SendMessage(update.Message.Chat.Id, "Choose an action", cancellationToken: ct,
                            replyMarkup: MainMenu());
                        break;
                    case "/open_position":
                        if (parts.Length < 3)
                            return;
                        var assetSymbol = parts[1];
                        var sourceSymbol = parts[2];

                        var opened = await positionService.OpenPosition(assetSymbol, sourceSymbol);
                        if (opened)
                        {
                            await bot.SendMessage(
                                update.Message.Chat.Id,
                                $"✅ Position opened for {assetSymbol}{sourceSymbol}",
                                cancellationToken: ct);
                        }
                        else
                        {
                            await bot.SendMessage(
                                update.Message.Chat.Id,
                                $"⚠️ Position already exists for {assetSymbol}{sourceSymbol}",
                                cancellationToken: ct);
                        }

                        break;

                    case "/close_position":
                        if (parts.Length < 2)
                            return;
                        assetSymbol = parts[1];
                        var exited = await positionService.ClosePosition(assetSymbol);
                        if (exited)
                        {
                            await bot.SendMessage(
                                update.Message.Chat.Id,
                                $"✅ Successfully exited position for {assetSymbol}",
                                cancellationToken: ct);
                        }
                        else
                        {
                            await bot.SendMessage(
                                update.Message.Chat.Id,
                                $"⚠️ No active position found for {assetSymbol}",
                                cancellationToken: ct);
                        }

                        break;
                }
            }
        }
        catch (Exception ex)
        {
            var message = update.Message ?? update.CallbackQuery?.Message;
            await bot.SendMessage(
                message.Chat.Id,
                $"⚠️ Error: {ex.Message}",
                cancellationToken: ct);
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient bot,
        Exception exception,
        CancellationToken ct)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}