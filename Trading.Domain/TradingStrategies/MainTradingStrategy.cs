using Trading.Domain.Aggregates.Position;
using Trading.Domain.Enums;
using Trading.Domain.EventArgs;
using Trading.Domain.Extensions;
using Trading.Domain.Helpers;
using Trading.Domain.ValueObjects;

namespace Trading.Domain.TradingStrategies;

public class MainTradingStrategy(
    decimal tradeValue,
    decimal takeProfitPercentage,
    decimal priceDeviationPercentage,
    decimal buyFeePercentage,
    decimal sellFeePercentage
)
{
    public event EventHandler<OrderPlacingEventArgs>? OrderPlacing;
    
    public event EventHandler<PositionFinishingEventArgs>? PositionFinishing;
    public event EventHandler<PositionFinishedEventArgs>? PositionFinished;

    public void PlaceOrders(Position position)
    {
        var conditionalOrders = CalculateConditionalOrders(position);
        if (conditionalOrders == null)
        {
            PositionFinishing?.Invoke(this, new PositionFinishingEventArgs(position));
            position.ClearWaitingOrders();
            PositionFinished?.Invoke(this, new PositionFinishedEventArgs(position));
            return;
        }

        var result = new List<ConditionalOrderRequest>();
        if (conditionalOrders.FinalSellOrder != null)
            result.Add(conditionalOrders.FinalSellOrder);
        if (conditionalOrders.DipBuyOrder != null)
            result.Add(conditionalOrders.DipBuyOrder);
        if (conditionalOrders.ShortSellOrder != null)
            result.Add(conditionalOrders.ShortSellOrder);

        if (result.Count == 0)
        {
            PositionFinished?.Invoke(this, new PositionFinishedEventArgs(position));
            return;
        }

        OrderPlacing?.Invoke(this, new OrderPlacingEventArgs(result, position));
    }

    // public List<ConditionalOrderRequest>? GetOrderRequests()
    // {
    //     var conditionalOrders = CalculateConditionalOrders();
    //     if (conditionalOrders == null)
    //         return null;
    //
    //     var result = new List<ConditionalOrderRequest>();
    //     if (conditionalOrders.FinalSellOrder != null)
    //         result.Add(conditionalOrders.FinalSellOrder);
    //     if (conditionalOrders.DipBuyOrder != null)
    //         result.Add(conditionalOrders.DipBuyOrder);
    //     if (conditionalOrders.ShortSellOrder != null)
    //         result.Add(conditionalOrders.ShortSellOrder);
    //
    //     return result.Count == 0 ? null : result;
    // }

    private ConditionalOrderRequestInfo? CalculateConditionalOrders(Position position)
    {
        var metrics = position.Metrics;

        if (metrics.Cost <= 1)// 1$ if position.SourceSymbol == "USD"
            return null; 
        
        var sellNetPrice = metrics.AverageNetPrice!.Value.IncreaseByPercentage(takeProfitPercentage);
        var sellGrossPrice = PriceHelper.CalculateGrossPriceForSell(sellNetPrice, sellFeePercentage);
        var finalSellOrder = new ConditionalOrderRequest
        {
            Symbol = position.Symbol,
            TriggerDirection = TriggerDirection.Rise,
            Quantity = metrics.Quantity,
            TriggerPrice = sellGrossPrice
        };

        var buyNetPrice = (metrics.Cost * tradeValue * (100m - priceDeviationPercentage)) /
                          (metrics.Quantity * (100m * tradeValue + metrics.Cost * priceDeviationPercentage));

        var buyGrossPrice = PriceHelper.CalculateGrossPriceForBuy(buyNetPrice, buyFeePercentage);
        var buyOrder = new ConditionalOrderRequest
        {
            Symbol = position.Symbol,
            TriggerDirection = TriggerDirection.Fall,
            Quantity = tradeValue / buyGrossPrice,
            TriggerPrice = buyGrossPrice
        };

        var shortSellNetPrice = (metrics.Cost * tradeValue * (1 + takeProfitPercentage / 100m)) /
                                (metrics.Quantity * (tradeValue + metrics.Cost * takeProfitPercentage / 100m));

        var shortSellGrossPrice = PriceHelper.CalculateGrossPriceForSell(shortSellNetPrice, sellFeePercentage);

        var shortSellOrder = new ConditionalOrderRequest
        {
            Symbol = position.Symbol,
            TriggerDirection = TriggerDirection.Rise,
            Quantity = tradeValue.IncreaseByPercentage(takeProfitPercentage) / shortSellGrossPrice,
            TriggerPrice = buyGrossPrice
        };

        return new ConditionalOrderRequestInfo
        {
            FinalSellOrder = finalSellOrder,
            DipBuyOrder = buyOrder,
            ShortSellOrder = shortSellOrder
        };

    }


    private record ConditionalOrderRequestInfo
    {
        public ConditionalOrderRequest? FinalSellOrder { get; init; }
        public ConditionalOrderRequest? DipBuyOrder { get; init; }
        public ConditionalOrderRequest? ShortSellOrder { get; init; }
    }
}