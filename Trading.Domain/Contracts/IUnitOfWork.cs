using Trading.Domain.Aggregates.Portfolio;
using Trading.Domain.Contracts;

namespace Trading.Domain.Contracts;


public interface IUnitOfWork : IDisposable
{
    IPortfolioRepository Portfolios { get; }

    Task<int> CompleteAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
}