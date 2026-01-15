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

    private MainTradingStrategy _strategy;
    const decimal BuyFeePercentage = 0.1m;
    const decimal SellFeePercentage = 0.1m;
    const decimal TradeValue = 100m;


    [SetUp]
    public void Setup()
    {
        _strategy = new MainTradingStrategy(
            tradeValue: 100m,
            takeProfitPercentage: 1m,
            priceDeviationPercentage: 1m,
            buyFeePercentage: BuyFeePercentage,
            sellFeePercentage: SellFeePercentage);

        _strategy.OrderPlacing += StrategyOnOrderPlacing;

        void StrategyOnOrderPlacing(object? sender, OrderPlacingEventArgs e)
        {
            BuyOrder = e.OrderRequests.FirstOrDefault(r => r.TriggerDirection == TriggerDirection.Fall);
            SellOrder = e.OrderRequests.FirstOrDefault(r => r.TriggerDirection == TriggerDirection.Rise);
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
        FillOrder(BuyOrder, position);

        _strategy.PlaceOrders(position);
        FillOrder(SellOrder, position);

        var averageNetPriceAfter = position.Metrics.AverageNetPrice!.Value;
        Assert.That(Math.Abs(averageNetPriceBefore - averageNetPriceAfter) / averageNetPriceAfter,
            Is.LessThan(0.000000000001m));

        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);

        averageNetPriceBefore = position.Metrics.AverageNetPrice!.Value;

        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);

        _strategy.PlaceOrders(position);
        FillOrder(SellOrder, position);

        averageNetPriceAfter = position.Metrics.AverageNetPrice!.Value;
        Assert.That(Math.Abs(averageNetPriceBefore - averageNetPriceAfter) / averageNetPriceAfter,
            Is.LessThan(0.000000000001m));
    }
    
    [Test]
    public void ShortSell_2()
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
        FillOrder(BuyOrder, position);
        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);
        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);
        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);
        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);
        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);
        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);
        _strategy.PlaceOrders(position);
        FillOrder(BuyOrder, position);

        _strategy.PlaceOrders(position);
        while (SellOrder != null)
        {
            
            FillOrder(BuyOrder, position);
            _strategy.PlaceOrders(position);
            
            FillOrder(SellOrder, position);
            _strategy.PlaceOrders(position);
            
            FillOrder(SellOrder, position);
            _strategy.PlaceOrders(position);
        }
    }


    private void FillOrder(ConditionalOrderRequest order, Position position)
    {
        if (order.TriggerDirection == TriggerDirection.Fall)
        {
            position.Buy("Buy", order.Quantity, order.TriggerPrice, BuyFeePercentage, DateTime.UtcNow);
        }
        else //RISE
        {
            position.Sell("Sell", order.Quantity, order.TriggerPrice, SellFeePercentage, DateTime.UtcNow);
        }

        BuyOrder = null;
        SellOrder = null;
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