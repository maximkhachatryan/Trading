using Trading.Base;
using Trading.Constants;

namespace Trading.Abstraction;

public interface IExchange
{
    Task<Kline[]> GetKlines(string symbol, Interval interval, int totalLimit, DateTime? endTime = null);
}