using MongoDB.Driver;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;
using Trading.Domain.ValueObjects;
using Trading.Infrastructure.Persistence.MongoDB.Configuration;
using Trading.Infrastructure.Persistence.MongoDB.Models;

namespace Trading.Infrastructure.Persistence.MongoDB.Repositories;

public class FinishedPositionRepository : IFinishedPositionRepository
{
    private readonly IMongoCollection<FinishedPositionDocument> _collection;

    public FinishedPositionRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<FinishedPositionDocument>(MongoDbCollections.FinishedPositions);
        
        // Create indexes for common queries
        CreateIndexes();
    }

    public async Task AddAsync(Position position)
    {
        var document = ToDocument(position);
        await _collection.InsertOneAsync(document);
    }

    public async Task<IEnumerable<Position>> GetBySymbolAsync(string symbol, int limit = 50)
    {
        var filter = Builders<FinishedPositionDocument>.Filter.Eq(p => p.Symbol, symbol);
        var documents = await _collection.Find(filter)
            .SortByDescending(p => p.FinishedAt)
            .Limit(limit)
            .ToListAsync();

        return documents.Select(ToDomain);
    }

    public async Task<IEnumerable<Position>> GetAllAsync(DateTime? from = null, DateTime? to = null, int skip = 0, int limit = 100)
    {
        var filterBuilder = Builders<FinishedPositionDocument>.Filter;
        var filter = filterBuilder.Empty;

        if (from.HasValue)
        {
            filter &= filterBuilder.Gte(p => p.FinishedAt, from.Value);
        }

        if (to.HasValue)
        {
            filter &= filterBuilder.Lte(p => p.FinishedAt, to.Value);
        }

        var documents = await _collection.Find(filter)
            .SortByDescending(p => p.FinishedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return documents.Select(ToDomain);
    }

    private void CreateIndexes()
    {
        // Index on Symbol for quick lookups by symbol
        var symbolIndexModel = new CreateIndexModel<FinishedPositionDocument>(
            Builders<FinishedPositionDocument>.IndexKeys.Ascending(p => p.Symbol)
        );

        // Index on FinishedAt for date range queries
        var finishedAtIndexModel = new CreateIndexModel<FinishedPositionDocument>(
            Builders<FinishedPositionDocument>.IndexKeys.Descending(p => p.FinishedAt)
        );

        // Compound index on Symbol + FinishedAt for common query pattern
        var compoundIndexModel = new CreateIndexModel<FinishedPositionDocument>(
            Builders<FinishedPositionDocument>.IndexKeys
                .Ascending(p => p.Symbol)
                .Descending(p => p.FinishedAt)
        );

        _collection.Indexes.CreateMany(new[]
        {
            symbolIndexModel,
            finishedAtIndexModel,
            compoundIndexModel
        });
    }

    private static FinishedPositionDocument ToDocument(Position position)
    {
        var metrics = position.Metrics;
        
        return new FinishedPositionDocument
        {
            Symbol = position.Symbol,
            SourceSymbol = position.SourceSymbol,
            AssetSymbol = position.AssetSymbol,
            Trades = position.Trades.Select(t => new FinishedTradeDocument
            {
                OrderId = t.OrderId,
                TimeStamp = t.TimeStamp,
                ActionType = t.ActionType,
                NetPrice = t.NetPrice,
                Quantity = t.Quantity
            }).ToList(),
            FinishedAt = DateTime.UtcNow,
            TotalProfit = metrics.Cost,
            TotalQuantity = metrics.Quantity,
            AverageCost = metrics.AverageNetPrice
        };
    }

    private static Position ToDomain(FinishedPositionDocument document)
    {
        var trades = document.Trades.Select(t => new Trade
        {
            OrderId = t.OrderId,
            TimeStamp = t.TimeStamp,
            ActionType = t.ActionType,
            NetPrice = t.NetPrice,
            Quantity = t.Quantity
        }).ToList();

        return Position.Reconstruct(
            document.SourceSymbol,
            document.AssetSymbol,
            trades,
            Enumerable.Empty<ConditionalOrder>()
        );
    }
}
