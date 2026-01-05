using Trading.Domain.Enums;

namespace Trading.Domain.Aggregates.Position;

public record Trade
{
    public string OrderId { get; init; } = null!;
    public DateTime TimeStamp { get; init; }
    public TradeActionType ActionType { get; init; }
    public decimal NetPrice { get; init; }
    public decimal Quantity { get; init; }
    
}