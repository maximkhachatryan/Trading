using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trading.Bot;
using Trading.Bot.Extensions;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var serviceProvider = new ServiceCollection()
    .ConfigureServices(configuration)
    .BuildServiceProvider();

var botService = serviceProvider.GetRequiredService<TelegramBotService>();
botService.Start();

Console.WriteLine("Press Ctrl+C to exit");
await Task.Delay(Timeout.Infinite);