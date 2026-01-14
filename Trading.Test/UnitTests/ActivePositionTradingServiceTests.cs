using Microsoft.Extensions.Options;
using NUnit.Framework;
using Trading.ApplicationServices.Configurations;
using Trading.ApplicationServices.Services;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Enums;
using Trading.Domain.Events;
using Trading.Domain.ValueObjects;
using Trading.Test.Mocks;

namespace Trading.Test.UnitTests;

[TestFixture]
public class ActivePositionTradingServiceTests
{
    private MockExchange _exchange;
    private MockActivePositionRepository _repository;
    private IOptions<ActivePositionTradingOptions> _options;
    private ActivePositionTradingService _service;

    [SetUp]
    public void Setup()
    {
        _exchange = new MockExchange();
        _repository = new MockActivePositionRepository();
        
        var options = new ActivePositionTradingOptions
        {
            TradeValue = 100,
            TakeProfitPercentage = 10m,
            PriceDeviationPercentage = 10m,
            BuyFeePercentage = 0.1m,
            SellFeePercentage = 0.1m
        };
        _options = Options.Create(options);

        _service = new ActivePositionTradingService(_exchange, _repository, _options);
    }

    [Test]
    public async Task StartTrading_ShouldCancelUntriggeredOrders_And_SubscribeToUpdates()
    {
        // Act
        await _service.StartTrading();

        // Assert
        // We know CancelAllUntriggeredConditionalSpotOrder should be called.
        // MockExchange logic clears conditional orders when called, so we can verify the state.
        
        // Actually, MockExchange default state is empty, so let's pre-fill it to verify cancellation
        await _exchange.PlaceConditionalOrder("BTCUSDT", OrderSide.Buy, 1, 50000, TriggerDirection.Fall);
        
        // Re-run service to trigger cancel
        await _service.StartTrading(); 
        
        var remainingOrders = await _exchange.GetUntriggeredConditionalSpotOrderIds();
        Assert.That(remainingOrders, Is.Empty);
    }
    
    [Test]
    public async Task ProcessUnhandledOrders_ShouldHandleWaitingOrders()
    {
        // Arrange
        var position = new Position
        {
            AssetSymbol = "BTC",
            SourceSymbol = "USDT"
        };
        _repository.AddPosition(position);
        
        // Add a waiting order to the position
        var order = await _exchange.PlaceConditionalOrder(position.Symbol, OrderSide.Buy, 1, 50000, TriggerDirection.Fall);
        position.AddWaitingOrder(order);
        
        // Simulate the order being filled
        _exchange.SimulateOrderFilled(order.OrderId, OrderSide.Buy, 49000);

        // Act
        // StartTrading triggers ProcessUnhandledOrders internally
        await _service.StartTrading();

        // Assert
        var updatedPosition = await _repository.GetActivePosition(position.Symbol);
        Assert.That(updatedPosition, Is.Not.Null);
        Assert.That(updatedPosition.WaitingOrders, Is.Empty); // Should be cleared
        Assert.That(updatedPosition.Trades, Has.Count.EqualTo(1)); // Should have a trade
        Assert.That(updatedPosition.Trades.First().ActionType, Is.EqualTo(TradeActionType.Buy));
    }

    [Test]
    public async Task HandleOrderFilled_BuyOrder_ShouldUpdatePosition_And_PlaceStrategyOrders()
    {
        // Arrange
        var position = new Position
        {
            AssetSymbol = "BTC",
            SourceSymbol = "USDT"
        };
        _repository.AddPosition(position);
        
        await _service.StartTrading();

        // Act
        // Simulate a buy filled event
        _exchange.SimulateOrderFilled("ORDER-1", OrderSide.Buy, 50000, position.Symbol, 1m);

        // Assert
        var updatedPosition = await _repository.GetActivePosition(position.Symbol);
        Assert.That(updatedPosition.Trades, Has.Count.EqualTo(1));
        Assert.That(updatedPosition.Trades.First().ActionType, Is.EqualTo(TradeActionType.Buy));
        
        // Verify strategy placed orders (conditional orders in exchange)
        // Use Delayed Constraint to allow async void event handler to complete
        Assert.That(async () => await _exchange.GetUntriggeredConditionalSpotOrderIds(position.Symbol), 
            Has.Some.Not.Empty.After(200, 10)); 
    }
    
    [Test]
    public async Task HandleOrderFilled_SellOrder_ShouldUpdatePosition_And_PlaceStrategyOrders()
    {
        // Arrange
        var position = new Position
        {
            AssetSymbol = "BTC",
            SourceSymbol = "USDT"
        };
        // Initial buy to allow selling
        position.Buy("INIT-BUY", 1, 50000, 0.001m, DateTime.UtcNow);
        _repository.AddPosition(position);

        await _service.StartTrading();

        // Act
        // Selling 1 eliminates the position
        _exchange.SimulateOrderFilled("ORDER-SELL", OrderSide.Sell, 55000, position.Symbol, 1m);

        // Assert
        // Allow time for async processing (PositionFinishing -> TryRemove)
        // Position should be removed (closed)
        Assert.That(async () => await _repository.GetActivePosition(position.Symbol), Is.Null.After(200, 10));
    }
    
    [Test]
    public async Task PlacingOrders_WithTargetPrices_Scenario()
    {
        // Arrange
        var position = new Position { AssetSymbol = "ETH", SourceSymbol = "USDT" };
        _repository.AddPosition(position);
        await _service.StartTrading();
        
        // Act: Buy at 1000
        _exchange.SimulateOrderFilled("BUY-1000", OrderSide.Buy, 1000, position.Symbol, 1m);
        
        // Assert
        // Wait for orders to be placed
        Assert.That(async () =>
        {
            var result = await _exchange.GetPlacedConditionalOrders(position.Symbol);
            return result;
        }, Has.Count.GreaterThan(0).After(200, 10));

        var placedOrders = await _exchange.GetPlacedConditionalOrders(position.Symbol);
        
        Assert.That(placedOrders, Has.Some.Matches<ConditionalOrder>(o => 
            o.TriggerDirection == TriggerDirection.Fall && 
            o.TriggerPrice < 1000m), "Expected Buy Dip Order below 1000");

        Assert.That(placedOrders, Has.Some.Matches<ConditionalOrder>(o => 
            o.TriggerDirection == TriggerDirection.Rise && 
            o.TriggerPrice > 1000m), "Expected Sell TakeProfit Order above 1000");
    }
    
    [Test]
    public async Task PlacingOrders_WithTargetPrices_Scenario1()
    {
        // Arrange
        var position = new Position { AssetSymbol = "ETH", SourceSymbol = "USDT" };
        _repository.AddPosition(position);
        await _service.StartTrading();
        
        // Act: Buy at 1000
        _exchange.SimulateOrderFilled("BUY-1000", OrderSide.Buy, 1000, position.Symbol, 1m);
        
        var placedOrders = await _exchange.GetPlacedConditionalOrders(position.Symbol);


        var sellOrder = placedOrders.First(x => x.OrderId == "MOCK-1");
        _exchange.SimulateOrderFilled(sellOrder.OrderId, OrderSide.Sell, sellOrder.TriggerPrice, position.Symbol, sellOrder.Quantity);


        var positionAfterSell = await _repository.GetActivePosition("ETHUSDT");
        
    }
}
