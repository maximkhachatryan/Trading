using Trading.Domain.Aggregates.Position;

namespace Trading.Domain.Contracts;

public interface IFinishedPositionRepository
{
    Task AddAsync(Position position);
    Task<IEnumerable<Position>> GetBySymbolAsync(string symbol, int limit = 50);
    Task<IEnumerable<Position>> GetAllAsync(DateTime? from = null, DateTime? to = null, int skip = 0, int limit = 100);
}
