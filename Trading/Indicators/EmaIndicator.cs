using Skender.Stock.Indicators;
using Trading.Abstraction;
using Trading.Base;

namespace Trading.Indicators;

public class EmaIndicator(int period) : IIndicator
{
    public int Period => period;
    
    public EmaResult[] Evaluate(Kline[] klines)
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

        return quotes.GetEma(period).ToArray();
    }

    public IEnumerable<IIndicator> GetFamousIndicatorList()
    {
        throw new NotImplementedException();
    }
}