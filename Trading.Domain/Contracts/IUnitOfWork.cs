using Trading.Domain.Aggregates.Portfolio;
using Trading.Domain.Contracts;

namespace Trading.Domain.Contracts;

public interface IUnitOfWork : IDisposable
{
    public IRepository<Portfolio> PortfolioRepository { get; }

    public IRepository<PortfolioAsset> PortfolioAssetRepository { get; }

    Task CompleteAsync();
}