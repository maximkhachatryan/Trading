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

        var buyOrder = conditionalOrders.DipBuyOrder;
        var sellOrder = new[]
            {
                conditionalOrders.FinalSellOrder,
                conditionalOrders.ShortSellOrder
            }
            .Where(o => o is not null)
            .MinBy(o => o!.TriggerPrice);

        var result = new List<ConditionalOrderRequest>();

        if (buyOrder != null)
            result.Add(buyOrder);
        if (sellOrder != null)
            result.Add(sellOrder);

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

        if (metrics.Cost <= 1) // 1$ if position.SourceSymbol == "USD"
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


        var shortSellOrder = CalculateShortSellOrderReducingAveragePrice(position);

        return new ConditionalOrderRequestInfo
        {
            FinalSellOrder = finalSellOrder,
            DipBuyOrder = buyOrder,
            ShortSellOrder = shortSellOrder
        };
    }

    private ConditionalOrderRequest? CalculateShortSellOrderReducingCost(Position position)
    {
        var metrics = position.Metrics;
        ConditionalOrderRequest shortSellOrder = null;
        var netShortSellAmount = tradeValue.IncreaseByPercentage(priceDeviationPercentage);
        if (metrics.Cost >= 2 * netShortSellAmount)
        {
            var shortSellNetPrice = netShortSellAmount / (metrics.Quantity *
                                                          (1m - ((metrics.Cost - netShortSellAmount) *
                                                                 (100m - priceDeviationPercentage)) /
                                                              (100m * metrics.Cost)));
            var shortSellGrossPrice = PriceHelper.CalculateGrossPriceForSell(shortSellNetPrice, sellFeePercentage);

            shortSellOrder = new ConditionalOrderRequest
            {
                Symbol = position.Symbol,
                TriggerDirection = TriggerDirection.Rise,
                Quantity = netShortSellAmount / shortSellNetPrice,
                TriggerPrice = shortSellGrossPrice
            };
        }

        return shortSellOrder;
    }

    private ConditionalOrderRequest? CalculateShortSellOrderReducingAveragePrice(Position position)
    {
        var metrics = position.Metrics;
        ConditionalOrderRequest shortSellOrder = null;
        if (metrics.Cost >= 2 * tradeValue)
        {
            var shortSellNetPrice = tradeValue /
                                    (metrics.Quantity *
                                     (1m - ((metrics.Cost - tradeValue) *
                                            (100m - priceDeviationPercentage)) /
                                         (metrics.Cost * (100m - priceDeviationPercentage / 2))));
            var shortSellGrossPrice = PriceHelper.CalculateGrossPriceForSell(shortSellNetPrice, sellFeePercentage);

            shortSellOrder = new ConditionalOrderRequest
            {
                Symbol = position.Symbol,
                TriggerDirection = TriggerDirection.Rise,
                Quantity = tradeValue / shortSellNetPrice,
                TriggerPrice = shortSellGrossPrice
            };
        }

        return shortSellOrder;
    }


    private record ConditionalOrderRequestInfo
    {
        public ConditionalOrderRequest? FinalSellOrder { get; init; }
        public ConditionalOrderRequest? DipBuyOrder { get; init; }
        public ConditionalOrderRequest? ShortSellOrder { get; init; }
    }
}