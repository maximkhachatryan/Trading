using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;
using Trading.Infrastructure.Persistence.FileStorage.Models;
using Trading.Domain.ValueObjects;

namespace Trading.Infrastructure.Persistence.FileStorage.Repositories;

public class ActivePositionRepository() : IActivePositionRepository
{
    private static readonly string FileName = "ActivePositions.json";

    public async Task<Dictionary<string, Position>> GetActivePositions()
    {
        var result = await FilePersistence.LoadAsync<Dictionary<string, PositionModel>>(FileName);
        if (result == null)
            return new Dictionary<string, Position>();

        return result.ToDictionary(k => k.Key, v => ToDomain(v.Value));
    }

    public async Task<Position?> GetActivePosition(string symbol)
    {
        var activePositions = await GetActivePositions();
        return activePositions.GetValueOrDefault(symbol);
    }

    public async Task<bool> TryAdd(Position position)
    {
        var activePositionsModels = await LoadModels();
        if (activePositionsModels.ContainsKey(position.Symbol))
        {
            return false;
        }

        activePositionsModels[position.Symbol] = ToModel(position);

        await FilePersistence.SaveAsync(activePositionsModels, FileName);
        return true;
    }
    
    public async Task<bool> TryUpdate(Position position)
    {
        var activePositionsModels = await LoadModels();
        if (!activePositionsModels.ContainsKey(position.Symbol))
        {
            return false;
        }

        activePositionsModels[position.Symbol] = ToModel(position);

        await FilePersistence.SaveAsync(activePositionsModels, FileName);
        return true;
    }

    public async Task<bool> TryRemove(string symbol)
    {
        var activePositionsModels = await LoadModels();
        if (!activePositionsModels.Remove(symbol))
        {
            return false;
        }

        await FilePersistence.SaveAsync(activePositionsModels, FileName);
        return true;
    }

    private async Task<Dictionary<string, PositionModel>> LoadModels()
    {
        var result = await FilePersistence.LoadAsync<Dictionary<string, PositionModel>>(FileName);
        return result ?? new Dictionary<string, PositionModel>();
    }

    private static PositionModel ToModel(Position position)
    {
        return new PositionModel
        {
            SourceSymbol = position.SourceSymbol,
            AssetSymbol = position.AssetSymbol,
            Trades = position.Trades.Select(t => new TradeModel
            {
                OrderId = t.OrderId,
                TimeStamp = t.TimeStamp,
                ActionType = t.ActionType,
                NetPrice = t.NetPrice,
                Quantity = t.Quantity
            }).ToList(),
            WaitingOrders = position.WaitingOrders.Select(o => new ConditionalOrderModel
            {
                OrderId = o.OrderId,
                Symbol = o.Symbol,
                Quantity = o.Quantity,
                TriggerPrice = o.TriggerPrice,
                PlacedAt = o.PlacedAt
            }).ToList()
        };
    }

    private static Position ToDomain(PositionModel model)
    {
        var trades = model.Trades.Select(t => new Trade
        {
            OrderId = t.OrderId,
            TimeStamp = t.TimeStamp,
            ActionType = t.ActionType,
            NetPrice = t.NetPrice,
            Quantity = t.Quantity
        }).ToList();

        var waitingOrders = model.WaitingOrders.Select(o => new ConditionalOrder
        {
            OrderId = o.OrderId,
            Symbol = o.Symbol,
            Quantity = o.Quantity,
            TriggerPrice = o.TriggerPrice,
            PlacedAt = o.PlacedAt
        });

        return Position.Reconstruct(model.SourceSymbol, model.AssetSymbol, trades, waitingOrders);
    }
}