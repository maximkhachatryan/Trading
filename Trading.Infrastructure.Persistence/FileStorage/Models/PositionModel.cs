using Trading.Domain.Enums;

namespace Trading.Infrastructure.Persistence.FileStorage.Models;

public class PositionModel
{
    public string SourceSymbol { get; set; } = string.Empty;
    public string AssetSymbol { get; set; } = string.Empty;
    public List<ConditionalOrderModel> WaitingOrders { get; set; } = new();
    public List<TradeModel> Trades { get; set; } = new();
}

public class ConditionalOrderModel
{
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TriggerPrice { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class TradeModel
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public TradeActionType ActionType { get; set; }
    public decimal NetPrice { get; set; }
    public decimal Quantity { get; set; }
}
