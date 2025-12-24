using Trading.Domain.Aggregates.Portfolio;
using Trading.Domain.Contracts;

namespace Trading.Test.Mocks;

public class MockPortfolioRepository : IPortfolioRepository
{
    private Portfolio? _currentPortfolio = new Portfolio("USDT", 100000, "PEPE");

    public Task<Portfolio?> GetActivePortfolio()
    {
        
        return Task.FromResult(_currentPortfolio);
    }
}