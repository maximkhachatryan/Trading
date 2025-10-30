using Skender.Stock.Indicators;
using Trading.Abstraction;
using Trading.Base;

namespace Trading.Indicators;

public class DemaIndicator(int period) : IIndicator
{
    public int Period { get; } = period;

    public DemaResult[] Evaluate(Kline[] klines)
    {
        var quotes = klines.Select(k => new Quote
        {
            Date = k.StartTime,
            Open = k.OpenPrice,
            High = k.HighPrice,
            Low = k.LowPrice,
            Close = k.ClosePrice,
            Volume = k.Volume
        }).ToList();

        return quotes.GetDema(period).ToArray();
    }

    public IEnumerable<IIndicator> GetFamousIndicatorList()
    {
        // Common DEMA settings often used in trading
        yield return new DemaIndicator(20);
        yield return new DemaIndicator(50);
        yield return new DemaIndicator(100);
        yield return new DemaIndicator(200);
    }
}