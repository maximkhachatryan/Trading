using Trading.Domain.Constants;
using Trading.Domain.Enums;
using Trading.Domain.Events;
using Trading.Domain.ValueObjects;

namespace Trading.ApplicationContracts;

public interface IExchange
{
    Task<Kline[]> GetKlines(string symbol, Interval interval, int totalLimit, DateTime? endTime = null);
    Task<List<string>> GetUntriggeredConditionalSpotOrderIds(string? symbol = null);
    Task<bool> CancelAllUntriggeredConditionalSpotOrder(string? symbol = null);
    Task<ConditionalOrder> PlaceConditionalBuyOrder(string symbol, decimal quantity, decimal triggerPrice, TriggerDirection triggerDirection);
    Task SubscribeToOrderUpdates(Action<OrderFilledEvent> onOrderFilled);
}