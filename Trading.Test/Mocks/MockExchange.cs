using Trading.ApplicationContracts;
using Trading.Domain.Constants;
using Trading.Domain.ValueObjects;

namespace Trading.Test.Mocks;

public class MockExchange : IExchange
{
    private readonly List<Kline> _klines = new();
    private readonly Dictionary<string, List<string>> _conditionalOrders = new();

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
}
