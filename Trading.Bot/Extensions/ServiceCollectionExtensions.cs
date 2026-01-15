using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Services;
using Trading.ApplicationServices;
using Trading.ApplicationServices.Configurations;
using Trading.ApplicationServices.Services;
using Trading.Domain.Contracts;
using Trading.Infrastructure.Persistence.FileStorage.Repositories;

namespace Trading.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    internal static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .RegisterServices(configuration)
            .RegisterRepositories();
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ActivePositionTradingOptions>(
            configuration.GetSection(ActivePositionTradingOptions.SectionName));
            
        services.AddScoped<IActivePositionService, ActivePositionService>();
        services.AddSingleton<IActivePositionTradingService, ActivePositionTradingService>();
        services.AddSingleton<IExchange, BybitExchange>();
        
        // Register TelegramNotifier
        var botToken = "8246739182:AAG49Dna-5Xfm6gCZCs8IXAranAKBW8R6Pk"; // Consistent with Program.cs
        var notifier = new TelegramNotifier(botToken);
        services.AddSingleton<INotifier>(notifier);
        services.AddSingleton(notifier); // Register as itself to allow Setting ChatId
        
        services.AddSingleton(sp => new TelegramBotService(botToken, sp, notifier));
        
        return services;
    }
    
    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IActivePositionRepository, ActivePositionRepository>();
        return services;
    }
}