using Trading.Domain.Constants;
using Trading.Domain.ValueObjects;

namespace Trading.ApplicationContracts;

public interface IExchange
{
    Task<Kline[]> GetKlines(string symbol, Interval interval, int totalLimit, DateTime? endTime = null);
}