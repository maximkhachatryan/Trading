using Trading.Abstraction;
using Trading.Base;
using Trading.Indicators;

namespace Trading.Strategies;

public class BenoStrategy(RSIIndicator rsiIndicator, EmaIndicator emaIndicator) : IStrategy
{
    private bool _inPosition;
    private bool _cooledDown = true;
    private decimal _enterPrice;

    public void Evaluate(Kline[] klines)
    {
        var rsiResults = rsiIndicator.Evaluate(klines);

        Kline[] a = new Kline[rsiResults.Length];
        try
        {
            a = rsiResults.Select(x => new Kline
            {
                StartTime = x.Date,
                ClosePrice = ((decimal?)x.Rsi) ?? 0m  
            }).ToArray();
        }
        catch (Exception ex)
        {
        }

        var emaResults = emaIndicator.Evaluate(a);

        var emaRsiResult = emaResults[emaResults.Length - 1];

        var prevEmaRsiResult = emaResults[emaResults.Length - 2];

        var rsiResult = rsiResults[rsiResults.Length - 1];

        var prevRsiResult = rsiResults[rsiResults.Length - 2];

        if (!_inPosition && _cooledDown)
        {
            var prevRsiHigherThanEma = prevRsiResult.Rsi!.Value > prevEmaRsiResult.Ema!.Value;
            var currentRsiHigherThanEma = rsiResult.Rsi!.Value > emaRsiResult.Ema!.Value;
            var deltaRsiMoreThan10 = prevRsiResult.Rsi!.Value - rsiResult.Rsi!.Value > -10;

            if (prevRsiHigherThanEma && currentRsiHigherThanEma && deltaRsiMoreThan10)
            {
                _enterPrice = klines.Last().ClosePrice;
                _inPosition = true;
                _cooledDown = false;
                OnBuySignal?.Invoke(klines.Last());
            }
        }
        else
        {
            var prevRsiLowerThanEma = prevRsiResult.Rsi!.Value < prevEmaRsiResult.Ema!.Value;
            var currentRsiLowerThanEma = rsiResult.Rsi!.Value < emaRsiResult.Ema!.Value;

            var currentPrice = klines.Last().ClosePrice;
            var takeProfitHit = _enterPrice + _enterPrice * 0.015m >= currentPrice;

            if ((prevRsiLowerThanEma && currentRsiLowerThanEma) || takeProfitHit)
            {
                _inPosition = false;

                OnSellSignal?.Invoke(klines.Last());
            }
        }

        if (!_cooledDown && rsiResult.Rsi < emaRsiResult.Ema && prevRsiResult.Rsi < prevEmaRsiResult.Ema)
        {
            _cooledDown = true;
        }
    }

    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}