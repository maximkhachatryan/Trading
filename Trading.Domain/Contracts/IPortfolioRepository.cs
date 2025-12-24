using Trading.Domain.Aggregates.Portfolio;

namespace Trading.Domain.Contracts;

public interface IPortfolioRepository
{
    Task<Portfolio?> GetActivePortfolio();
}