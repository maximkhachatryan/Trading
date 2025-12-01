using MongoDB.Driver;
using Trading.Domain.Contracts;
using Trading.Infrastructure.Persistence.MongoDB.Repositories;

namespace Trading.Infrastructure.Persistence.MongoDB;

public class MongoUnitOfWork:IUnitOfWork
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private IClientSessionHandle? _session;
    private bool _disposed = false;
    
    public IPortfolioRepository Portfolios { get; }
    

    public MongoUnitOfWork(IMongoClient client, IMongoDatabase database)
    {
        _client = client;
        _database = database;

        // All repositories automatically use the current session (if any)
        Portfolios = new PortfolioRepository(_database, () => _session);
    }
    
    
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _session = await _client.StartSessionAsync(cancellationToken: ct);
        _session.StartTransaction();
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_session?.IsInTransaction == true)
            await _session.CommitTransactionAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_session?.IsInTransaction == true)
            await _session.AbortTransactionAsync(ct);
    }

    // EF Core compatibility — returns 1 on success
    public Task<int> CompleteAsync(CancellationToken ct = default)
        => Task.FromResult(_session?.IsInTransaction == true ? 1 : 0);

    public void Dispose()
    {
        if (_disposed) return;
        _session?.Dispose();
        _disposed = true;
    }
}