using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Services;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;
using Trading.Domain.Enums;
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
        var cancelSucceeded = await exchange.CancelAllUntriggeredConditionalSpotOrder();
    
        if (!cancelSucceeded)
        {
            Console.WriteLine("Trading couldn't be started (couldn't cancel UntriggeredConditionalSpotOrders)");
            return;
        }
        
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

    private async Task HandleOrderFilled(OrderFilledEvent orderFilledEvent, Position activePosition)
    {
        var strategy = new MainTradingStrategy(
            activePosition,
            TradeValue,
            TakeProfitPercentage,
            PriceDeviationPercentage,
            BuyFeePercentage,
            SellFeePercentage
        );

        if (orderFilledEvent.Side == OrderSide.Buy)
        {
            strategy.Buy(orderFilledEvent.OrderId, orderFilledEvent.Quantity, orderFilledEvent.ExecutionPrice, orderFilledEvent.FilledAt);
        }
        else if (orderFilledEvent.Side == OrderSide.Sell)
        {
            strategy.Sell(orderFilledEvent.OrderId, orderFilledEvent.Quantity, orderFilledEvent.ExecutionPrice, orderFilledEvent.FilledAt);
        }

        await activePositionRepository.TryUpdate(activePosition);

        var newOrderRequests = strategy.GetOrderRequests();
        await exchange.CancelAllUntriggeredConditionalSpotOrder(activePosition.Symbol);

        if (newOrderRequests == null)//Position finished
        {
            await activePositionRepository.TryRemove(activePosition.Symbol);
            //TODO: Save position into history
            return;
        }
        try
        {
            foreach (var orderRequest in newOrderRequests)
            {
                await exchange.PlaceConditionalOrder(symbol: orderRequest.Symbol,
                    orderRequest.TriggerDirection == TriggerDirection.Fall ? OrderSide.Buy : OrderSide.Sell,
                    orderRequest.Quantity, orderRequest.TriggerPrice, orderRequest.TriggerDirection);
            }
        }
        catch (Exception e)
        {
            await exchange.CancelAllUntriggeredConditionalSpotOrder(activePosition.Symbol);
        }
        
        
    }
}