using Trading.ApplicationContracts;
using Trading.Domain.Constants;
using Trading.Domain.Events;
using Trading.Domain.ValueObjects;

namespace Trading.Test.Mocks;

public class MockExchange : IExchange
{
    private readonly List<Kline> _klines = new();
    private readonly Dictionary<string, List<string>> _conditionalOrders = new();
    private readonly Dictionary<string, ConditionalOrder> _placedOrders = new();
    private readonly Dictionary<string, OrderFilledEvent> _filledOrders = new();
    private Action<OrderFilledEvent>? _orderFilledCallback;
    private int _orderIdCounter = 1;

    public void AddKlines(params Kline[] klines)
    {
        _klines.AddRange(klines);
    }

    public void AddConditionalOrder(string symbol, string orderId)
    {
        if (!_conditionalOrders.ContainsKey(symbol))
        {
            _conditionalOrders[symbol] = new List<string>();
        }
        _conditionalOrders[symbol].Add(orderId);
    }

    public void ClearKlines()
    {
        _klines.Clear();
    }

    public void ClearConditionalOrders()
    {
        _conditionalOrders.Clear();
        _placedOrders.Clear();
    }

    public Task<Kline[]> GetKlines(string symbol, Interval interval, int totalLimit, DateTime? endTime = null)
    {
        var filteredKlines = _klines
            .Where(k => endTime == null || k.StartTime <= endTime.Value)
            .OrderByDescending(k => k.StartTime)
            .Take(totalLimit)
            .OrderBy(k => k.StartTime)
            .ToArray();

        return Task.FromResult(filteredKlines);
    }

    public Task<List<string>> GetUntriggeredConditionalSpotOrderIds(string? symbol = null)
    {
        if (symbol == null)
        {
            var allOrders = _conditionalOrders.Values
                .SelectMany(orders => orders)
                .ToList();
            return Task.FromResult(allOrders);
        }

        if (_conditionalOrders.TryGetValue(symbol, out var orders))
        {
            return Task.FromResult(new List<string>(orders));
        }

        return Task.FromResult(new List<string>());
    }

    public Task<bool> CancelAllUntriggeredConditionalSpotOrder(string? symbol = null)
    {
        if (symbol == null)
        {
            _conditionalOrders.Clear();
            return Task.FromResult(true);
        }

        if (_conditionalOrders.ContainsKey(symbol))
        {
            _conditionalOrders.Remove(symbol);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<ConditionalOrder> PlaceConditionalOrder(string symbol, Domain.Enums.OrderSide side, decimal quantity, decimal triggerPrice, Domain.Enums.TriggerDirection triggerDirection)
    {
        var orderId = $"MOCK-{_orderIdCounter++}";
        
        var order = new ConditionalOrder
        {
            OrderId = orderId,
            Symbol = symbol,
            Quantity = quantity,
            TriggerPrice = triggerPrice,
            TriggerDirection = triggerDirection,
            PlacedAt = DateTime.UtcNow
        };

        _placedOrders[orderId] = order;
        AddConditionalOrder(symbol, orderId);

        return Task.FromResult(order);
    }

    public Task SubscribeToOrderUpdates(Action<OrderFilledEvent> onOrderFilled)
    {
        _orderFilledCallback = onOrderFilled;
        return Task.CompletedTask;
    }

    public Task<OrderFilledEvent?> GetFilledOrderById(string orderId)
    {
        // Use the simulation dictionary or fallback to on-the-fly generation if needed
        // But better is to use a dictionary populated by SimulateOrderFilled
        if (_filledOrders.TryGetValue(orderId, out var filledEvent))
        {
            return Task.FromResult<OrderFilledEvent?>(filledEvent);
        }
        
        var filledOrder = _klines
            .Select(k => new OrderFilledEvent
            {
                OrderId = orderId,
                Symbol = "BTCUSDT", // Simplification for mock
                ExecutionPrice = k.ClosePrice,
                FilledAt = k.StartTime,
                Quantity = 1, // Default quantity
                Side = Domain.Enums.OrderSide.Buy // Default side, usually logic needs more context
            })
            .FirstOrDefault(); 

        return Task.FromResult<OrderFilledEvent?>(filledOrder);
    }

    /// <summary>
    /// Helper method for testing: simulates an order being filled
    /// </summary>
    public void SimulateOrderFilled(string orderId, Domain.Enums.OrderSide side, decimal executionPrice, string? symbol = null, decimal? quantity = null)
    {
        if (!_placedOrders.TryGetValue(orderId, out var order) && symbol == null)
        {
             // If order not found in placed orders (e.g. created manually in test), proceed with minimal data
             // Or throw exception, but for flexibility let's try to handle key info
             // throw new InvalidOperationException($"Order {orderId} not found");
        }

        var resolvedSymbol = order?.Symbol ?? symbol ?? "UNKNOWN";
        var resolvedQuantity = order?.Quantity ?? quantity ?? 0;

        var filledEvent = new OrderFilledEvent
        {
            OrderId = orderId,
            Symbol = resolvedSymbol,
            Side = side,
            Quantity = resolvedQuantity,
            ExecutionPrice = executionPrice,
            FilledAt = DateTime.UtcNow
        };

        _filledOrders[orderId] = filledEvent;
        _orderFilledCallback?.Invoke(filledEvent);
        
        // Remove from conditional orders as it's now filled
        if (order != null && _conditionalOrders.TryGetValue(order.Symbol, out var orders))
        {
            orders.Remove(orderId);
        }
    }

    public Task<List<ConditionalOrder>> GetPlacedConditionalOrders(string symbol)
    {
        var orders = _placedOrders.Values.Where(o => o.Symbol == symbol).ToList();
        return Task.FromResult(orders);
    }
}
