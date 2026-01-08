using Trading.ApplicationContracts.Dtos.Position;
using Trading.ApplicationContracts.Services;
using Trading.Domain.Contracts;

namespace Trading.ApplicationServices.Services;

public class ActivePositionService(IActivePositionRepository activePositionRepository) : IActivePositionService
{
    public Task<bool> OpenPosition(string symbol, decimal sourceAmount)
    {
        return Task.FromResult(true);
    }

    public Task ExitPosition(string symbol)
    {
        return Task.CompletedTask;
    }

    public async Task<PositionDetailsDto?> GetOpenPosition(string symbol)
    {
        var activePosition = await activePositionRepository.GetActivePosition(symbol);

        if (activePosition == null)
        {
            return null;
        }

        var metrics = activePosition.Metrics;
        return new PositionDetailsDto
        {
            AssetSymbol = activePosition.AssetSymbol,
            SourceSymbol = activePosition.SourceSymbol,
            AverageNetPrice = metrics.AverageNetPrice,
            Cost = metrics.Cost,
            Quantity = metrics.Quantity
        };
    }

    public async Task<Dictionary<string, PositionDetailsDto>> GetOpenPositions()
    {
        var activePositions = await activePositionRepository.GetActivePositions();

        return activePositions.Select(kv => KeyValuePair.Create(kv.Key, new PositionDetailsDto
        {
            AssetSymbol = kv.Value.AssetSymbol,
            SourceSymbol = kv.Value.SourceSymbol,
            AverageNetPrice = kv.Value.Metrics.AverageNetPrice,
            Cost = kv.Value.Metrics.Cost,
            Quantity = kv.Value.Metrics.Quantity
        })).ToDictionary();
    }
}