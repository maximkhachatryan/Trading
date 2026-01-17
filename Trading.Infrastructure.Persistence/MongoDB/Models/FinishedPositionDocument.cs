using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Trading.Infrastructure.Persistence.MongoDB.Models;

public class FinishedPositionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    public string Symbol { get; set; } = string.Empty;
    public string SourceSymbol { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    
    public List<FinishedTradeDocument> Trades { get; set; } = new();
    
    public DateTime FinishedAt { get; set; }
    
    // Calculated metrics for easy querying and reporting
    public decimal TotalProfit { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal? AverageCost { get; set; }
}
