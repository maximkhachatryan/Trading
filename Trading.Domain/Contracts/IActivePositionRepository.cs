using Trading.Domain.Aggregates.Position;

namespace Trading.Domain.Contracts;

public interface IActivePositionRepository
{
    Task<Dictionary<string, Position>> GetActivePositions();
    Task<Position?> GetActivePosition(string symbol);
}