using Trading.ApplicationContracts.Dtos.Position;

namespace Trading.ApplicationContracts.Services;

public interface IActivePositionService
{
    public Task<bool> OpenPosition(string symbol, decimal sourceAmount);
    public Task<bool> ExitPosition(string symbol);
    public Task<PositionDetailsDto?> GetOpenPosition(string symbol);
    public Task<Dictionary<string, PositionDetailsDto>> GetOpenPositions();
}