using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Trading.Domain.Enums;
using Trading.Domain.Extensions;
using Trading.Domain.Helpers;
using Trading.Domain.ValueObjects;

namespace Trading.Domain.Aggregates.Position;

public class Position
{
    private readonly Dictionary<string, ConditionalOrder> _waitingOrders = new();

    public IReadOnlyCollection<ConditionalOrder> WaitingOrders => _waitingOrders.Values;
    public string SourceSymbol { get; set; } = null!;
    public string AssetSymbol { get; set; } = null!;

    public string Symbol => $"{AssetSymbol}{SourceSymbol}";

    public static Position Reconstruct(string sourceSymbol, string assetSymbol, List<Trade> trades, IEnumerable<ConditionalOrder> waitingOrders)
    {
        var position = new Position
        {
            SourceSymbol = sourceSymbol,
            AssetSymbol = assetSymbol,
            Trades = trades
        };
        
        foreach (var order in waitingOrders)
        {
            position.AddWaitingOrder(order);
        }
        
        return position;
    }


    public void Buy(string orderId, decimal quantity, decimal grossPrice, decimal buyFeePercentage, DateTime timestamp)
    {
        if (Trades.Any(t => t.OrderId == orderId)) return;
        
        var netPrice = PriceHelper.CalculateNetPriceForBuy(grossPrice, buyFeePercentage);
        var netQuantity = quantity.DecreaseByPercentage(buyFeePercentage);
        
        Trades.Add(new Trade
        {
            OrderId = orderId,
            TimeStamp = timestamp,
            ActionType = TradeActionType.Buy,
            NetPrice = netPrice,
            Quantity = netQuantity
        });
        _waitingOrders.Remove(orderId);
    }

    public void Sell(string orderId, decimal quantity, decimal grossPrice, decimal sellFeePercentage, DateTime timestamp)
    {
        if (Trades.Any(t => t.OrderId == orderId)) return;
        
        var netPrice = PriceHelper.CalculateNetPriceForSell(grossPrice, sellFeePercentage);
        Trades.Add(new Trade
        {
            OrderId = orderId,
            TimeStamp = timestamp,
            ActionType = TradeActionType.Sell,
            NetPrice = netPrice,
            Quantity = quantity
        });
        _waitingOrders.Remove(orderId);
    }

    public void AddWaitingOrder(ConditionalOrder order) => _waitingOrders[order.OrderId] = order;


    public void ClearWaitingOrders() => _waitingOrders.Clear();


    public List<Trade> Trades { get; private set; } = [];

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