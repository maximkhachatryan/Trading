using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;

namespace Trading.Infrastructure.Persistence.FileStorage.Repositories;

public class ActivePositionRepository() : IActivePositionRepository
{
    private static readonly string FileName = "ActivePositions.json";
    public async Task<Dictionary<string, Position>> GetActivePositions()
    {
        var result = await FilePersistence.LoadAsync<Dictionary<string, Position>>(FileName);
        return result ?? new Dictionary<string, Position>();
    }

    public async Task<Position?> GetActivePosition(string symbol)
    {
        var activePositions = await GetActivePositions();
        return activePositions.GetValueOrDefault(symbol);
    }

    public async Task<bool> TryAdd(Position position)
    {
        var activePositions = await GetActivePositions();
        if (!activePositions.TryAdd(position.AssetSymbol, position))
        {
            return false;
        }

        await FilePersistence.SaveAsync(activePositions, FileName);
        return true;
    }

    public async Task<bool> TryRemove(string symbol)
    {
        var activePositions = await GetActivePositions();
        if (!activePositions.Remove(symbol))
        {
            return false;
        }

        await FilePersistence.SaveAsync(activePositions, FileName);
        return true;
    }
}