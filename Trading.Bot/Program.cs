using Microsoft.Extensions.DependencyInjection;
using Trading.Bot;
using Trading.Bot.Extensions;

var serviceProvider = new ServiceCollection()
    .ConfigureServices()
    .BuildServiceProvider();

// var botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
//                ?? throw new Exception("Missing TELEGRAM_BOT_TOKEN");
var botToken = "8246739182:AAG49Dna-5Xfm6gCZCs8IXAranAKBW8R6Pk";

var botService = new TelegramBotService(botToken, serviceProvider);
botService.Start();

Console.WriteLine("Press Ctrl+C to exit");
await Task.Delay(Timeout.Infinite);