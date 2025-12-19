using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Trading.Domain.Contracts;
using Trading.Worker.Configurations;

namespace Trading.Worker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDb"));

        // Register MongoClient as singleton
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        // Register database
        services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return client.GetDatabase(settings.DatabaseName);
        });

        //services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        //services.AddScoped<IUnitOfWork, MongoUnitOfWork>();

        return services;
    }
}