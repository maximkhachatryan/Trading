using Skender.Stock.Indicators;
using Trading.Abstraction;
using Trading.Base;

namespace Trading.Indicators;

public class RSIIndicator(int period = 14) : IIndicator
{
    
    public RsiResult[] Evaluate(Kline[] klines)
    {
        var quotes = klines.Select(k => new Quote
        {
            Date = k.StartTime,
            Open = k.OpenPrice,
            High = k.HighPrice,
            Low = k.LowPrice,
            Close = k.ClosePrice,
            Volume = k.Volume
        }).ToArray();

        return quotes.GetRsi(period).ToArray();
    }

    public IEnumerable<IIndicator> GetFamousIndicatorList()
    {
        throw new NotImplementedException();
    }
}