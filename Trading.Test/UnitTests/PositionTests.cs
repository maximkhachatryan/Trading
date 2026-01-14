using Trading.Domain.Aggregates.Position;

namespace Trading.Test.UnitTests;

public class PositionTests
{
    const decimal BuyFeePercentage= 0.1m;
    const decimal SellFeePercentage= 0.1m;
    

    [SetUp]
    public void Setup()
    {
    }
    
    [Test]
    public void Buy_Metrics()
    {
        var position = new Position
        {
            SourceSymbol = "USDT",
            AssetSymbol = "ETH"
        };

        position.Buy("1st buy", 1, 1000m, BuyFeePercentage, DateTime.UtcNow);

        var metrics = position.Metrics;

        Assert.That(metrics.Cost.Equals(1000m));
        Assert.That(metrics.Quantity.Equals((1m)*(1m-BuyFeePercentage/100)));
        Assert.That(metrics.AverageNetPrice.Equals(1000m/((1m)*(1m-BuyFeePercentage/100m))));
        
        position.Buy("2nd buy", 2, 2000m, BuyFeePercentage, DateTime.UtcNow);
        
        metrics = position.Metrics;

        Assert.That(metrics.Cost.Equals(5000m));
        Assert.That(metrics.Quantity.Equals((1m+2m)*(1m-BuyFeePercentage/100)));
        Assert.That(metrics.AverageNetPrice.Equals(5000m/((1m+2m)*(1m-BuyFeePercentage/100m))));
        
    }
    
    
    [Test]
    public void Sell_Metrics()
    {
        var position = new Position
        {
            SourceSymbol = "USDT",
            AssetSymbol = "ETH"
        };

        position.Buy("1st buy", 1m, 1000m, BuyFeePercentage, DateTime.UtcNow);

        position.Sell("1st sell", 0.5m, 2000m, SellFeePercentage, DateTime.UtcNow);
        
        var metrics = position.Metrics;

        Assert.That(metrics.Cost.Equals(1m));
        Assert.That(metrics.Quantity.Equals(0.499m));
        Assert.That(metrics.AverageNetPrice.Equals(1m/0.499m));
        
    }
}