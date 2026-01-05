using Trading.Domain.Aggregates.Position;
using Trading.Domain.Enums;
using Trading.Domain.Extensions;
using Trading.Domain.Helpers;
using Trading.Domain.ValueObjects;

namespace Trading.Domain.TradingStrategies;

public class MainTradingStrategy(
    Position position,
    decimal tradeValue,
    decimal takeProfitPercentage,
    decimal priceDeviationPercentage,
    decimal buyFeePercentage,
    decimal sellFeePercentage
)
{
    public void Buy(string orderId, decimal quantity, decimal grossPrice, DateTime timestamp)
    {
        var netPrice = PriceHelper.CalculateNetPriceForBuy(grossPrice, buyFeePercentage);
        position.Buy(orderId, quantity, netPrice, timestamp);
    }
    
    public void Sell(string orderId, decimal quantity, decimal grossPrice, DateTime timestamp)
    {
        var netPrice = PriceHelper.CalculateNetPriceForSell(grossPrice, sellFeePercentage);
        position.Sell(orderId, quantity, netPrice, timestamp);
    }

    public List<ConditionalOrderRequest>? GetOrderRequests()
    {
        var conditionalOrders = CalculateConditionalOrders();
        if (conditionalOrders == null )
            return null;

        var result = new List<ConditionalOrderRequest>();
        if (conditionalOrders.FinalSellOrder != null)
            result.Add(conditionalOrders.FinalSellOrder);
        if (conditionalOrders.DipBuyOrder != null)
            result.Add(conditionalOrders.DipBuyOrder);
        if (conditionalOrders.ShortSellOrder != null)
            result.Add(conditionalOrders.ShortSellOrder);

        return result.Count == 0 ? null : result;
    }
    
    private ConditionalOrderRequestInfo? CalculateConditionalOrders()
    {
        var metrics = position.Metrics;

        if (metrics.Cost > 1)// 1$ if position.SourceSymbol == "USD"
        {
            var sellNetPrice = metrics.AverageNetPrice!.Value.IncreaseByPercentage(takeProfitPercentage);
            var sellGrossPrice = PriceHelper.CalculateGrossPriceForSell(sellNetPrice, sellFeePercentage);
            var finalSellOrder = new ConditionalOrderRequest
            {
                Symbol = position.AssetSymbol,
                TriggerDirection = TriggerDirection.Rise,
                Quantity = metrics.Quantity,
                TriggerPrice = sellGrossPrice
            };

            var buyNetPrice = (metrics.Cost * tradeValue * (100m - priceDeviationPercentage)) /
                              (metrics.Quantity * (100m * tradeValue + metrics.Cost * priceDeviationPercentage));

            var buyGrossPrice = PriceHelper.CalculateGrossPriceForBuy(buyNetPrice, buyFeePercentage);
            var buyOrder = new ConditionalOrderRequest
            {
                Symbol = position.AssetSymbol,
                TriggerDirection = TriggerDirection.Fall,
                Quantity = tradeValue / buyGrossPrice,
                TriggerPrice = buyGrossPrice
            };
            
            var shortSellNetPrice = (metrics.Cost * tradeValue * (1 + takeProfitPercentage / 100m)) /
                                    (metrics.Quantity * (tradeValue + metrics.Cost * takeProfitPercentage / 100m));

            var shortSellGrossPrice = PriceHelper.CalculateGrossPriceForSell(shortSellNetPrice, sellFeePercentage);
            
            var shortSellOrder = new ConditionalOrderRequest
            {
                Symbol = position.AssetSymbol,
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

        return null;
    }
    
    
    private record ConditionalOrderRequestInfo
    {
        public ConditionalOrderRequest? FinalSellOrder { get; init; }
        public ConditionalOrderRequest? DipBuyOrder { get; init; }
        public ConditionalOrderRequest? ShortSellOrder { get; init; }
    }
}