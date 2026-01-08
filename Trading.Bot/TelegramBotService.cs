using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Trading.ApplicationContracts.Services;

namespace Trading.Bot;

public class TelegramBotService(
    string botToken,
    IServiceProvider serviceProvider)
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
                    var symbol = parts[1];
                    if (!decimal.TryParse(parts[2], out var amount))
                    {
                        return;
                    }
                    
                    var opened = await positionService.OpenPosition(symbol, amount);
                    if (opened)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            $"✅ Position opened for {symbol} with {amount} USDT",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            $"⚠️ Position already exists for {symbol}",
                            cancellationToken: ct);
                    }
                    break;

                case "/exit_position":
                    if (parts.Length < 2)
                        return;
                    symbol = parts[1];
                    var exited = await positionService.ExitPosition(symbol);
                    if (exited)
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            $"✅ Successfully exited position for {symbol}",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(
                            message.Chat.Id,
                            $"⚠️ No active position found for {symbol}",
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