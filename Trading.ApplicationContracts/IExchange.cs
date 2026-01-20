using Trading.Domain.Constants;
using Trading.Domain.Enums;
using Trading.Domain.Events;
using Trading.Domain.ValueObjects;

namespace Trading.ApplicationContracts;

public interface IExchange
{
    Task<Kline[]> GetKlines(string symbol, Interval interval, int totalLimit, DateTime? endTime = null);
    Task<decimal> GetCurrentPrice(string symbol);
    Task<List<string>> GetUntriggeredConditionalSpotOrderIds(string? symbol = null);
    Task<bool> CancelAllUntriggeredConditionalSpotOrder(string? symbol = null);
    Task<ConditionalOrder> PlaceConditionalOrder(string symbol, OrderSide side, decimal quantity, decimal triggerPrice);
    Task<OrderFilledEvent?> PlaceMarketOrder(string symbol, OrderSide side, decimal quantity);
    Task<ExchangeSubscriptionResult> SubscribeToOrderUpdates(Action<OrderFilledEvent> onOrderFilled);
    Task<OrderFilledEvent?> GetFilledOrderById(string orderId);
}