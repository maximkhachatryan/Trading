using Microsoft.Extensions.Options;
using NUnit.Framework;
using Trading.ApplicationContracts;
using Trading.ApplicationServices;
using Trading.ApplicationServices.Configurations;
using Trading.Domain.Constants;
using Trading.Domain.Enums;

namespace Trading.Test.IntegrationTests;

[TestFixture]
[Explicit("Integration tests requiring real Bybit credentials")]
public class BybitExchangeTests
{
    private IExchange _exchange;

    [SetUp]
    public void Setup()
    {
        var options = Options.Create(new BybitOptions
        {
            ApiKey = "WJdIxvOSmRx35kwbRs",
            ApiSecret = "B72zAcsP4D7BK7nA80nGbPwHQa1jvD2dmbuI",
            UseTestnet = true
        });
        _exchange = new BybitExchange(options);
    }

    [Test]
    public async Task GetKlines_ShouldReturnData()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var interval = Interval.OneHour;
        var limit = 10;

        // Act
        var klines = await _exchange.GetKlines(symbol, interval, limit);

        // Assert
        Assert.That(klines, Is.Not.Null);
        Assert.That(klines, Is.Not.Empty);
        Assert.That(klines.Length, Is.EqualTo(limit));
        
        foreach (var kline in klines)
        {
            Assert.That(kline.ClosePrice, Is.GreaterThan(0));
            Assert.That(kline.Volume, Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public async Task PlaceConditionalOrder_CheckTriggerDirection()
    {
        var symbol = "ETHUSDT";
        var quantity = 0.001m; 
        var triggerPrice = 10000m;
        var side = OrderSide.Buy;
        
        var order1 = await _exchange.PlaceConditionalOrder(symbol, side, quantity, triggerPrice);
        
        symbol = "ETHUSDT";
        quantity = 0.001m; 
        triggerPrice = 9000;
        side = OrderSide.Buy;
        
        var order2 = await _exchange.PlaceConditionalOrder(symbol, side, quantity, triggerPrice);
        
        
    }

    [Test]
    public async Task PlaceConditionalOrder_And_Cancel_ShouldWork()
    {
        // Arrange
        var symbol = "BTCUSDT";
        // Ensure quantity is valid for the symbol (min order size)
        var quantity = 0.001m; 
        // Trigger price far away to avoid filling
        var triggerPrice = 10000m; 
        var side = OrderSide.Buy;

        // Act
        // 1. Place Order
        var order = await _exchange.PlaceConditionalOrder(symbol, side, quantity, triggerPrice);

        // Assert Placement
        Assert.That(order, Is.Not.Null);
        Assert.That(order.OrderId, Is.Not.Null.And.Not.Empty);

        // 2. Check Order Exists
        var openOrders = await _exchange.GetUntriggeredConditionalSpotOrderIds(symbol);
        Assert.That(openOrders, Does.Contain(order.OrderId));

        // 3. Cancel Orders
        var cancelled = await _exchange.CancelAllUntriggeredConditionalSpotOrder(symbol);
        Assert.That(cancelled, Is.True);

        // 4. Verify Empty
        var remainingOrders = await _exchange.GetUntriggeredConditionalSpotOrderIds(symbol);
        Assert.That(remainingOrders, Is.Empty);
    }

    [Test]
    public async Task GetUntriggeredConditionalSpotOrderIds_ShouldReturnList()
    {
        // Act
        var orders = await _exchange.GetUntriggeredConditionalSpotOrderIds("BTCUSDT");

        // Assert
        Assert.That(orders, Is.Not.Null);
        // Can't assert count > 0 unless we know we have orders, but we can verify it doesn't throw.
    }

    [Test]
    public async Task CancelAllUntriggeredConditionalSpotOrder_ShouldReturnTrue()
    {
        // Act
        var result = await _exchange.CancelAllUntriggeredConditionalSpotOrder("BTCUSDT");

        // Assert
        // Result depends on whether there were orders or API response, but strictly should usually return success from API perspective
        Assert.That(result, Is.True);
    }
}
