using Microsoft.Extensions.DependencyInjection;
using Trading.ApplicationContracts.Services;
using Trading.ApplicationServices.Services;
using Trading.Domain.Contracts;
using Trading.Infrastructure.Persistence.FileStorage.Repositories;

namespace Trading.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    internal static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        return services
            .RegisterServices()
            .RegisterRepositories();
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IActivePositionService, ActivePositionService>();
        return services;
    }
    
    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IActivePositionRepository, ActivePositionRepository>();
        return services;
    }
}