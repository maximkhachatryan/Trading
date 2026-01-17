using Trading.Domain.Enums;

namespace Trading.Infrastructure.Persistence.MongoDB.Models;

public class FinishedTradeDocument
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public TradeActionType ActionType { get; set; }
    public decimal NetPrice { get; set; }
    public decimal Quantity { get; set; }
}
