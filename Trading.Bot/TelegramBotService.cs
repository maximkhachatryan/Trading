using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var positionService = scope.ServiceProvider.GetRequiredService<IActivePositionService>();
            
        if (update.Type != UpdateType.Message)
            return;

        var message = update.Message;
        if (message?.Text == null)
            return;

        notifier.ChatId = message.Chat.Id;

        var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();

        try
        {
            switch (command)
            {
                case "/get_open_positions":
                    var openPositions = await positionService.GetOpenPositions();
                    if (openPositions.Count == 0)
                    {
                        await bot.SendMessage(message.Chat.Id, "No open positions.", cancellationToken: ct);
                    }
                    else
                    {
                        var response = "Open Positions:\n" + string.Join("\n", openPositions.Select(p => 
                            $"- {p.Key}: Qty {p.Value.Quantity:F4}, Avg Price {p.Value.AverageNetPrice:F2}, Cost {p.Value.Cost:F2} {p.Value.SourceSymbol}"));
                        await bot.SendMessage(message.Chat.Id, response, cancellationToken: ct);
                    }
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
                            message.Chat.Id,
                            $"✅ Position opened for {assetSymbol}{sourceSymbol}",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
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
                            message.Chat.Id,
                            $"✅ Successfully exited position for {assetSymbol}",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            $"⚠️ No active position found for {assetSymbol}",
                            cancellationToken: ct);
                    }
                    break;

                case "/start_trading":
                    var tradingService = scope.ServiceProvider.GetRequiredService<IActivePositionTradingService>();
                    var started = await tradingService.StartTrading();
                    if (started)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            "🚀 Trading started!",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            "❌ Failed to start trading. Check logs for details.",
                            cancellationToken: ct);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
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