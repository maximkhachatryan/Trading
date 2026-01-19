using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Services;
using Trading.ApplicationServices.Configurations;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;
using Trading.Domain.Enums;
using Trading.Domain.EventArgs;
using Trading.Domain.Events;
using Trading.Domain.TradingStrategies;
using Trading.Domain.ValueObjects;

namespace Trading.ApplicationServices.Services;

public class ActivePositionTradingService(
    IExchange exchange,
    IServiceScopeFactory scopeFactory,
    INotifier notifier,
    IOptions<ActivePositionTradingOptions> options,
    IFinishedPositionRepository finishedPositionRepository)
    : IActivePositionTradingService
{
    private readonly ActivePositionTradingOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isTrading;

    public async Task<bool> StartTrading()
    {
        //Cancel all spot conditional orders for all symbols.
        //Get all active positions
        //Get all trades from each position start date. Update position based on missed buy/sell trades. Don't forget to save position.
        //Start listening to the active positions symbols.
        //Place conditional orders

        if (_isTrading) return true;
        _isTrading = true;

        var cancelSucceeded = await exchange.CancelAllUntriggeredConditionalSpotOrder();

        if (!cancelSucceeded)
        {
            Console.WriteLine("Trading couldn't be started (couldn't cancel UntriggeredConditionalSpotOrders)");
            _isTrading = false;
            return false;
        }

        using (var scope = scopeFactory.CreateScope())
        {
            var activePositionRepository = scope.ServiceProvider.GetRequiredService<IActivePositionRepository>();
            await ProcessUnhandledOrders(activePositionRepository);
        }

        await notifier.Notify("🤖 Trading system ONLINE and monitoring active positions.");

        await exchange.SubscribeToOrderUpdates(async (orderFilledEvent) =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var activePositionRepository = scope.ServiceProvider.GetRequiredService<IActivePositionRepository>();

                var activePosition = await activePositionRepository.GetActivePosition(orderFilledEvent.Symbol);
                if (activePosition == null)
                {
                    Console.WriteLine(
                        $"Warning: No active position found for order with Id {orderFilledEvent.OrderId}");
                    return;
                }

                await HandleOrderFilled(orderFilledEvent, activePosition, activePositionRepository);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error in OrderUpdate callback: {ex.Message}";
                Console.WriteLine(errorMsg);
                await notifier.Notify($"❌ {errorMsg}");
            }
        });

        return true;
    }

    private async Task ProcessUnhandledOrders(IActivePositionRepository activePositionRepository)
    {
        var positions = await activePositionRepository.GetActivePositions();
        foreach (var position in positions.Values)
        {
            foreach (var waitingOrder in position.WaitingOrders.ToList())
            {
                var filledOrder = await exchange.GetFilledOrderById(waitingOrder.OrderId);

                if (filledOrder != null)
                {
                    await HandleOrderFilled(filledOrder, position, activePositionRepository);
                }
            }

            position.ClearWaitingOrders();
            await activePositionRepository.TryUpdate(position);
        }
    }

    private async Task HandleOrderFilled(OrderFilledEvent orderFilledEvent, Position activePosition,
        IActivePositionRepository activePositionRepository)
    {
        await _lock.WaitAsync();
        try
        {
            await ApplyOrderFilledAndProcessStrategy(orderFilledEvent, activePosition, activePositionRepository);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task ApplyOrderFilledAndProcessStrategy(OrderFilledEvent? orderFilledEvent, Position activePosition,
        IActivePositionRepository activePositionRepository)
    {
        if (orderFilledEvent != null)
        {
            if (activePosition.Trades.Any(t => t.OrderId == orderFilledEvent.OrderId))
            {
                return;
            }

            if (orderFilledEvent.Side == OrderSide.Buy)
            {
                activePosition.Buy(orderFilledEvent.OrderId, orderFilledEvent.Quantity, orderFilledEvent.ExecutionPrice,
                    _options.BuyFeePercentage, orderFilledEvent.FilledAt);
                await notifier.Notify(
                    $"🟢 BUY FILLED: {activePosition.Symbol}\nQty: {orderFilledEvent.Quantity}\nPrice: {orderFilledEvent.ExecutionPrice}");
            }
            else if (orderFilledEvent.Side == OrderSide.Sell)
            {
                activePosition.Sell(orderFilledEvent.OrderId, orderFilledEvent.Quantity,
                    orderFilledEvent.ExecutionPrice, _options.SellFeePercentage, orderFilledEvent.FilledAt);
                await notifier.Notify(
                    $"🔴 SELL FILLED: {activePosition.Symbol}\nQty: {orderFilledEvent.Quantity}\nPrice: {orderFilledEvent.ExecutionPrice}");
            }

            await activePositionRepository.TryUpdate(activePosition);
        }

        await exchange.CancelAllUntriggeredConditionalSpotOrder(activePosition.Symbol);

        var strategy = new MainTradingStrategy(
            _options.TradeValue,
            _options.TakeProfitPercentage,
            _options.PriceDeviationPercentage,
            _options.ShortSellDenominator,
            _options.BuyFeePercentage,
            _options.SellFeePercentage
        );
        
        strategy.OrderPlacing += async (s, e) =>
        {
            try 
            {
                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IActivePositionRepository>();
                await OnPlacingOrder(s, e, repo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OrderPlacing: {ex}");
            }
        };
        strategy.PositionFinishing += async (s, e) =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IActivePositionRepository>();
                await OnPositionFinishing(s, e, repo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PositionFinishing: {ex}");
            }
        };
        strategy.PositionFinished += async (s, e) =>
        {
            try
            {
                await OnPositionFinished(s, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PositionFinished: {ex}");
            }
        };

        strategy.Process(activePosition);
    }

    private async Task OnPositionFinishing(object? sender, PositionFinishingEventArgs eventArgs, IActivePositionRepository activePositionRepository)
    {
        await activePositionRepository.TryRemove(eventArgs.Position.Symbol);
        await notifier.Notify($"🏁 Position FINISHED: {eventArgs.Position.Symbol}");
    }
    
    private async Task OnPositionFinished(object? sender, PositionFinishedEventArgs eventArgs)
    {
        try
        {
            await finishedPositionRepository.AddAsync(eventArgs.Position);
            Console.WriteLine($"Position saved to history: {eventArgs.Position.Symbol}");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to save position to history for {eventArgs.Position.Symbol}: {ex.Message}";
            Console.WriteLine(errorMsg);
            await notifier.Notify($"⚠️ {errorMsg}");
        }
    }
    
    private async Task OnPlacingOrder(object? sender, OrderPlacingEventArgs eventArgs, IActivePositionRepository activePositionRepository)
    {
        try
        {
            var symbol = eventArgs.Position.Symbol;
            var currentPrice = await exchange.GetCurrentPrice(symbol);

            // Check if any order condition is already met
            ConditionalOrderRequest? triggeredOrderRequest = null;
            foreach (var orderRequest in eventArgs.OrderRequests)
            {
                var isConditionMet = (orderRequest.TriggerDirection == TriggerDirection.Fall
                                      && currentPrice <= orderRequest.TriggerPrice) ||
                                     (orderRequest.TriggerDirection == TriggerDirection.Rise
                                      && currentPrice >= orderRequest.TriggerPrice);

                if (isConditionMet)
                {
                    triggeredOrderRequest = orderRequest;
                    break;
                }
            }

            if (triggeredOrderRequest != null)
            {
                // Place a market order for the first triggered condition
                var side = triggeredOrderRequest.TriggerDirection == TriggerDirection.Fall
                    ? OrderSide.Buy
                    : OrderSide.Sell;
                await exchange.PlaceMarketOrder(symbol, side, triggeredOrderRequest.Quantity);

                return; // Skip placing other orders. We wait for the socket event to trigger actual strategy processing.
            }

            // If no conditions met, place all conditional orders
            foreach (var orderRequest in eventArgs.OrderRequests)
            {
                var order = await exchange.PlaceConditionalOrder(symbol: orderRequest.Symbol,
                    side: orderRequest.TriggerDirection == TriggerDirection.Fall ? OrderSide.Buy : OrderSide.Sell,
                    quantity: orderRequest.Quantity,
                    triggerPrice: orderRequest.TriggerPrice);

                eventArgs.Position.AddWaitingOrder(order);
            }
        }
        catch (Exception e)
        {
            var errorMsg = $"Error in OnPlacingOrder for {eventArgs.Position.Symbol}: {e.Message}";
            Console.WriteLine(errorMsg);
            await notifier.Notify($"❌ {errorMsg}");
            await exchange.CancelAllUntriggeredConditionalSpotOrder(eventArgs.Position.Symbol);
            eventArgs.Position.ClearWaitingOrders();
        }
        finally
        {
            await activePositionRepository.TryUpdate(eventArgs.Position);
        }
    }
}