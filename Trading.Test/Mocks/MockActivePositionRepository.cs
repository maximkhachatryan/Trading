using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;

namespace Trading.Test.Mocks;

public class MockActivePositionRepository : IActivePositionRepository
{
    private readonly Dictionary<string, Position> _positions = new();

    public Task<Dictionary<string, Position>> GetActivePositions()
    {
        return Task.FromResult(new Dictionary<string, Position>(_positions));
    }

    public Task<Position?> GetActivePosition(string symbol)
    {
        _positions.TryGetValue(symbol, out var position);
        return Task.FromResult(position);
    }

    public Task<bool> TryAdd(Position position)
    {
        if (_positions.ContainsKey(position.Symbol))
        {
            return Task.FromResult(false);
        }

        _positions[position.Symbol] = position;
        return Task.FromResult(true);
    }

    public Task<bool> TryUpdate(Position position)
    {
        if (!_positions.ContainsKey(position.Symbol))
        {
            return Task.FromResult(false);
        }

        _positions[position.Symbol] = position;
        return Task.FromResult(true);
    }

    public Task<bool> TryRemove(string symbol)
    {
        return Task.FromResult(_positions.Remove(symbol));
    }
    
    // Helper method for test setup
    public void AddPosition(Position position)
    {
        _positions[position.Symbol] = position;
    }
}
