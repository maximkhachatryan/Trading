using System.ComponentModel.DataAnnotations;
using Trading.Domain.Enums;

namespace Trading.Domain.Aggregates.Position;

public class Position
{
    public string SourceSymbol { get; set; } = null!;
    public string AssetSymbol { get; set; } = null!;

    public void Buy(string orderId, decimal quantity, decimal netPrice, DateTime timestamp)
    {
        Trades.Add(new Trade
        {
            OrderId = orderId,
            TimeStamp = timestamp,
            ActionType = TradeActionType.Buy,
            NetPrice = netPrice,
            Quantity = quantity
        });
    }
    
    public void Sell(string orderId, decimal quantity, decimal netPrice, DateTime timestamp)
    {
        Trades.Add(new Trade
        {
            OrderId = orderId,
            TimeStamp = timestamp,
            ActionType = TradeActionType.Sell,
            NetPrice = netPrice,
            Quantity = quantity
        });
    }
    
    public List<Trade> Trades { get; set; } = [];

    public (decimal Quantity, decimal Cost, decimal? AverageNetPrice) Metrics
    {
        get
        {
            if (Trades.Count == 0)
            {
                return (Quantity: 0, Cost: 0, AverageNetPrice: null);
            }
            
            var totalQuantity = 0m;
            var totalCost = 0m;

            foreach (var trade in Trades)
            {
                switch (trade.ActionType)
                {
                    case TradeActionType.Buy:
                        totalCost += trade.NetPrice * trade.Quantity;
                        totalQuantity += trade.Quantity;
                        break;
                    case TradeActionType.Sell:
                        totalCost -= trade.NetPrice * trade.Quantity;
                        totalQuantity -= trade.Quantity;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            decimal? averageNetPrice = totalQuantity > 0
                ? totalCost / totalQuantity
                : null;

            return (totalQuantity, totalCost, averageNetPrice);
        }
    }
}