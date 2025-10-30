using Skender.Stock.Indicators;
using Trading.Abstraction;
using Trading.Base;

namespace Trading.Indicators;

public class SupertrendIndicator(int atrPeriod, double multiplier) : IIndicator
{
    public int AtrPeriod { get; } = atrPeriod;
    public SuperTrendResult[] Evaluate(Kline[] klines)
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

        // Skender.Stock.Indicators has Supertrend built-in
        return quotes.GetSuperTrend(atrPeriod, multiplier).ToArray();
    }

    public IEnumerable<IIndicator> GetFamousIndicatorList()
    {
        // You could return common presets for backtesting
        yield return new SupertrendIndicator(7, 2.0);
        yield return new SupertrendIndicator(10, 3.0);
        yield return new SupertrendIndicator(14, 3.0);
        yield return new SupertrendIndicator(20, 4.0);
    }
}