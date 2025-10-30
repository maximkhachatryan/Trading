using Skender.Stock.Indicators;
using Trading.Abstraction;
using Trading.Base;

namespace Trading.Indicators;

public class MacdIndicator(int fastPeriods = 12, int slowPeriods = 26, int signalPeriods = 9) : IIndicator
{
    public MacdResult[] Evaluate(Kline[] klines)
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

        return quotes.GetMacd(fastPeriods, slowPeriods, signalPeriods).ToArray();
    }

    public IEnumerable<IIndicator> GetFamousIndicatorList()
    {
        var macdParamsCombinations = new List<(int Fast, int Slow, int Signal)>
        {
            (12, 26, 9),
            (5, 35, 5),
            (8, 17, 9),
            (19, 39, 9)
        };

        foreach (var comb in macdParamsCombinations)
        {
            yield return new MacdIndicator(comb.Fast, comb.Slow, comb.Signal);
        }
    }
}