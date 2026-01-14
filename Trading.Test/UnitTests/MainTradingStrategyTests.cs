using Trading.Domain.Aggregates.Position;
using Trading.Domain.Enums;
using Trading.Domain.EventArgs;
using Trading.Domain.TradingStrategies;
using Trading.Domain.ValueObjects;

namespace Trading.Test.UnitTests;

[TestFixture]
public class MainTradingStrategyTests
{
    private static List<TradeInfo> tradeInfos = [];

    private static ConditionalOrderRequest? BuyOrder = null;
    private static ConditionalOrderRequest? SellOrder = null;
    private static ConditionalOrderRequest? ShortSellOrder = null;


    private MainTradingStrategy _strategy;
    const decimal BuyFeePercentage = 0.1m;
    const decimal SellFeePercentage = 0.1m;
    const decimal TradeValue = 100m;


    [SetUp]
    public void Setup()
    {
        _strategy = new MainTradingStrategy(
            tradeValue: 100m,
            takeProfitPercentage: 10m,
            priceDeviationPercentage: 10m,
            buyFeePercentage: BuyFeePercentage,
            sellFeePercentage: SellFeePercentage);

        _strategy.OrderPlacing += StrategyOnOrderPlacing;

        void StrategyOnOrderPlacing(object? sender, OrderPlacingEventArgs e)
        {
            BuyOrder = e.OrderRequests.First(r => r.TriggerDirection == TriggerDirection.Fall);
            ShortSellOrder = e.OrderRequests.FirstOrDefault(r =>
                r.TriggerDirection == TriggerDirection.Rise && r.Quantity < e.Position.Metrics.Quantity);
            SellOrder = e.OrderRequests.First(r =>
                r.TriggerDirection == TriggerDirection.Rise && r.Quantity == e.Position.Metrics.Quantity);
        }
    }

    [Test]
    public void ShortSell()
    {
        var position = new Position
        {
            SourceSymbol = "USDT",
            AssetSymbol = "ETH"
        };
        var initialPrice = 120000m;
        position.Buy("1st buy", TradeValue / initialPrice, initialPrice, BuyFeePercentage, DateTime.UtcNow);

        var averageNetPriceBefore = position.Metrics.AverageNetPrice!.Value;

        _strategy.PlaceOrders(position);
        TradeOrder(BuyOrder, position);

        _strategy.PlaceOrders(position);
        TradeOrder(ShortSellOrder, position);

        var averageNetPriceAfter = position.Metrics.AverageNetPrice!.Value;
        Assert.That(Math.Abs(averageNetPriceBefore - averageNetPriceAfter) / averageNetPriceAfter,
            Is.LessThan(0.000000000001m));

        _strategy.PlaceOrders(position);
        TradeOrder(BuyOrder, position);

        averageNetPriceBefore = position.Metrics.AverageNetPrice!.Value;

        _strategy.PlaceOrders(position);
        TradeOrder(BuyOrder, position);

        _strategy.PlaceOrders(position);
        TradeOrder(ShortSellOrder, position);

        averageNetPriceAfter = position.Metrics.AverageNetPrice!.Value;
        Assert.That(Math.Abs(averageNetPriceBefore - averageNetPriceAfter) / averageNetPriceAfter,
            Is.LessThan(0.000000000001m));
    }


    private void TradeOrder(ConditionalOrderRequest order, Position position)
    {
        if (order.TriggerDirection == TriggerDirection.Fall)
        {
            position.Buy("Buy", order.Quantity, order.TriggerPrice, BuyFeePercentage, DateTime.UtcNow);
        }
        else //RISE
        {
            position.Sell("Sell", order.Quantity, order.TriggerPrice, SellFeePercentage, DateTime.UtcNow);
        }
    }
}

public class TradeInfo
{
    public decimal LastBuyPrice { get; set; }
    public decimal NextBuy { get; set; }
    public decimal NextSell { get; set; }
    public decimal RisePercent => (NextSell - LastBuyPrice) / LastBuyPrice * 100;
    public decimal DropPercent => (LastBuyPrice - NextBuy) / LastBuyPrice * 100;
}