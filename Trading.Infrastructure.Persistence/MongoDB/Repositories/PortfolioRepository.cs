using MongoDB.Driver;
using Trading.Domain.Aggregates.Portfolio;
using Trading.Domain.Contracts;

namespace Trading.Infrastructure.Persistence.MongoDB.Repositories;

public class PortfolioRepository(IMongoDatabase database, Func<IClientSessionHandle?> getSession)
    : IPortfolioRepository
{
    private IMongoCollection<Portfolio> Collection =>
        database.GetCollection<Portfolio>("orders")
            .WithReadConcern(ReadConcern.Snapshot)
            .WithReadPreference(ReadPreference.Primary)
            .WithWriteConcern(WriteConcern.WMajority);


    public async Task<Portfolio?> GetByIdAsync(int id, CancellationToken ct = default)
        => await Collection.Find(getSession(), o => o.Id == id).FirstOrDefaultAsync(ct);

    public async Task AddAsync(Portfolio portfolio, CancellationToken ct = default)
        => await Collection.InsertOneAsync(getSession(), portfolio, cancellationToken: ct);

    public async Task UpdateAsync(Portfolio portfolio, CancellationToken ct = default)
        => await Collection.ReplaceOneAsync(getSession(),
            o => o.Id == portfolio.Id, portfolio, cancellationToken: ct);
}