using Trading.ApplicationContracts.Dtos.Position;
using Trading.ApplicationContracts.Services;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;

namespace Trading.ApplicationServices.Services;

public class ActivePositionService(IActivePositionRepository activePositionRepository) : IActivePositionService
{
    public async Task<bool> OpenPosition(string symbol, decimal sourceAmount)
    {
        var position = new Position()
        {
            AssetSymbol = symbol,
            SourceSymbol = "USDT"
        };
        var assumedNetPrice = 90000m;
        position.Buy("1", sourceAmount / assumedNetPrice, assumedNetPrice, DateTime.UtcNow);
        return await activePositionRepository.TryAdd(position);
    }

    public async Task<bool> ExitPosition(string symbol)
    {
        return await activePositionRepository.TryRemove(symbol);
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