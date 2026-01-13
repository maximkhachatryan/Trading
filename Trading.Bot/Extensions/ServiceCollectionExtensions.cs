using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trading.ApplicationContracts.Services;
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
        services.AddScoped<IActivePositionTradingService, ActivePositionTradingService>(); // Assuming this is needed as per request
        return services;
    }
    
    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IActivePositionRepository, ActivePositionRepository>();
        return services;
    }
}