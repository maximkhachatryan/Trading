using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Services;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;
using Trading.Domain.Enums;
using Trading.Domain.EventArgs;
using Trading.Domain.Events;
using Trading.Domain.TradingStrategies;

namespace Trading.ApplicationServices.Services;

public class ActivePositionTradingService(
    IExchange exchange,
    IActivePositionRepository activePositionRepository)
    : IActivePositionTradingService
{
    private static readonly decimal TradeValue = 1m;
    private static readonly decimal TakeProfitPercentage = 1m;
    private static readonly decimal PriceDeviationPercentage = 1m;
    private static readonly decimal BuyFeePercentage = 1m;
    private static readonly decimal SellFeePercentage = 1m;

    public async Task StartTrading()
    {
        //Cancel all spot conditional orders for all symbols.
        //Get all active positions
        //Get all trades from each position start date. Update position based on missed buy/sell trades. Don't forget to save position.
        //Start listening to the active positions symbols.
        //Place conditional orders
        
        var cancelSucceeded = await exchange.CancelAllUntriggeredConditionalSpotOrder();
    
        if (!cancelSucceeded)
        {
            Console.WriteLine("Trading couldn't be started (couldn't cancel UntriggeredConditionalSpotOrders)");
            return;
        }

        await ProcessUnhandledOrders();
        
        await exchange.SubscribeToOrderUpdates(async void (orderFilledEvent) =>
        {
            var activePosition = await activePositionRepository.GetActivePosition(orderFilledEvent.Symbol);
            if (activePosition == null)
            {
                Console.WriteLine($"Warning: No active position found for order with Id {orderFilledEvent.OrderId}");
                return;
            }
    
            await HandleOrderFilled(orderFilledEvent, activePosition);
        });
    }

    private async Task ProcessUnhandledOrders()
    {
        var positions = await activePositionRepository.GetActivePositions();
        foreach (var position in positions.Values) 
        {
            foreach (var waitingOrder in position.WaitingOrders)
            {
                var filledOrder = await exchange.GetFilledOrderById(waitingOrder.OrderId);

                if (filledOrder != null)
                {
                    await HandleOrderFilled(filledOrder, position);
                }
            }
            position.ClearWaitingOrders();
        }
    }

    private async Task HandleOrderFilled(OrderFilledEvent orderFilledEvent, Position activePosition)
    {
        if (orderFilledEvent.Side == OrderSide.Buy)
        {
            activePosition.Buy(orderFilledEvent.OrderId, orderFilledEvent.Quantity, orderFilledEvent.ExecutionPrice, BuyFeePercentage, orderFilledEvent.FilledAt);
        }
        else if (orderFilledEvent.Side == OrderSide.Sell)
        {
            activePosition.Sell(orderFilledEvent.OrderId, orderFilledEvent.Quantity, orderFilledEvent.ExecutionPrice, SellFeePercentage, orderFilledEvent.FilledAt);
        }

        await activePositionRepository.TryUpdate(activePosition);

        await exchange.CancelAllUntriggeredConditionalSpotOrder(activePosition.Symbol);

        var strategy = new MainTradingStrategy(
            TradeValue,
            TakeProfitPercentage,
            PriceDeviationPercentage,
            BuyFeePercentage,
            SellFeePercentage
        );
        
        strategy.OrderPlacing += async (s, e) => await OnPlacingOrder(s, e);//Problem: Exceptions crash process
        strategy.PositionFinishing += async (s, e) => await OnPositionFinishing(s, e);//Problem: Exceptions crash process
        strategy.PositionFinished += async (s, e) => await OnPositionFinished(s, e);//Problem: Exceptions crash process
        
        strategy.PlaceOrders(activePosition);
    }

    private async Task OnPositionFinishing(object? sender, PositionFinishingEventArgs eventArgs)
    {
        await activePositionRepository.TryRemove(eventArgs.Position.Symbol);
    }
    
    private async Task OnPositionFinished(object? sender, PositionFinishedEventArgs eventArgs)
    {
        //TODO: Save position into history
    }
    
    private async Task OnPlacingOrder(object? sender, OrderPlacingEventArgs eventArgs)
    {
        try
        {
            foreach (var orderRequest in eventArgs.OrderRequests)
            {
                var order = await exchange.PlaceConditionalOrder(symbol: orderRequest.Symbol,
                    orderRequest.TriggerDirection == TriggerDirection.Fall ? OrderSide.Buy : OrderSide.Sell,
                    orderRequest.Quantity, orderRequest.TriggerPrice, orderRequest.TriggerDirection);

                eventArgs.Position.AddWaitingOrder(order);
            }
        }
        catch (Exception e)
        {
            await exchange.CancelAllUntriggeredConditionalSpotOrder(eventArgs.Position.Symbol);
            eventArgs.Position.ClearWaitingOrders();
        }
        finally
        {
            await activePositionRepository.TryUpdate(eventArgs.Position);
        }
    }
}