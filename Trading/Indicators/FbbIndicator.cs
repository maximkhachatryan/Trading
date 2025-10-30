using Skender.Stock.Indicators;
using Trading.Abstraction;
using Trading.Base;


namespace Trading.Indicators;

public sealed class FbbResult : ResultBase
{
    public FbbResult(DateTime date)
    {
        Date = date;
    }

    public decimal? MiddleBand { get; set; }
    public Dictionary<double, (decimal? Upper, decimal? Lower)> Bands { get; set; } = new();
}

public class FbbIndicator(int period, params double[] fibMultipliers) : IIndicator
{
    public int Period { get; } = period;
    private readonly double[] _multipliers = fibMultipliers.Length > 0 
        ? fibMultipliers 
        : new double[] { 1.618, 2.618, 4.236 };

    public FbbResult[] Evaluate(Kline[] klines)
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

        var sma = quotes.GetSma(period).ToArray();
        var stdDev = quotes.GetStdDev(period).ToArray();

        var results = new List<FbbResult>();

        for (int i = 0; i < quotes.Count; i++)
        {
            var res = new FbbResult(quotes[i].Date);

            if (sma[i].Sma is not null && stdDev[i].StdDev is not null)
            {
                res.MiddleBand = (decimal)sma[i].Sma.Value;

                foreach (var fib in _multipliers)
                {
                    var deviation = (decimal)(stdDev[i].StdDev.Value * fib);
                    var mb = res.MiddleBand.Value;

                    res.Bands[fib] = (mb + deviation, mb - deviation);
                }
            }

            results.Add(res);
        }

        return results.ToArray();
    }

    public IEnumerable<IIndicator> GetFamousIndicatorList()
    {
        throw new NotImplementedException();
    }
}
