using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Trading.Domain.Enums;
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


    public void Buy(string orderId, decimal quantity, decimal grossPrice, decimal buyFeePercentage, DateTime timestamp)
    {
        var netPrice = PriceHelper.CalculateNetPriceForBuy(grossPrice, buyFeePercentage);
        var netQuantity = quantity;
        
        //TODO: 
        // var netPrice = grossPrice;
        // var netQuantity = quantity * (1 - buyFeePercentage / 100);
        
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