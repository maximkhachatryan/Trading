using Trading.Domain.Enums;

namespace Trading.Domain.Aggregates.Position;

public class Trade
{
    public DateTime TimeStamp { get; set; }
    public TradeActionType ActionType { get; set; }
    public decimal NetPrice { get; set; }
    public decimal Quantity { get; set; }
    
}