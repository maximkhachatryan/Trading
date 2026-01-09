using Trading.ApplicationContracts.Dtos.Position;
using Trading.ApplicationContracts.Services;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;

namespace Trading.ApplicationServices.Services;

public class ActivePositionService(IActivePositionRepository activePositionRepository) : IActivePositionService
{
    public async Task<bool> OpenPosition(string symbol, decimal sourceAmount)
    {
        var position = new Position
        {
            AssetSymbol = symbol,
            SourceSymbol = "USDT"
        };
        return await activePositionRepository.TryAdd(position);
        //TODO: Start listening to the trades of opened position (to catch the trade will be done manually afterwards)
    }

    public async Task<bool> ExitPosition(string symbol)
    {
        //TODO: Remove all conditional orders
        //TODO: Sell assets of the given symbol by market price
        //TODO: Wait for 10 seconds for the last trade to be sent via socket
        //TODO: Gracefully unsubscribe from the socket
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

    public async Task StartTrading()
    {
        //Cancel all spot conditional orders for all symbols.
        //Get all active positions
        //Get all trades from each position start date. Update position based on missed buy/sell trades. Don't forget to save position.
        //Start listening to the active positions symbols.
        //Place conditional orders
    }
}