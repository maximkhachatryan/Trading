using Trading.Abstraction;
using Trading.Base;
using Trading.Indicators;

namespace Trading.Strategies;

public class DemaSupertrendStrategy(
    DemaIndicator fastDema,
    DemaIndicator slowDema,
    SupertrendIndicator supertrend)
    : IStrategy
{
    private bool _inPosition = false;

    public void Evaluate(Kline[] klines)
    {
        if (klines.Length < Math.Max(Math.Max(fastDema.Period, slowDema.Period), supertrend.AtrPeriod))
            return;

        var fastResults = fastDema.Evaluate(klines);
        var slowResults = slowDema.Evaluate(klines);
        var stResults = supertrend.Evaluate(klines);

        int last = klines.Length - 1;

        if (fastResults[last].Dema is null || slowResults[last].Dema is null || stResults[last].SuperTrend is null)
            return;

        decimal close = klines[last].ClosePrice;
        double fast = fastResults[last].Dema!.Value;
        double slow = slowResults[last].Dema!.Value;
        double super = (double)stResults[last].SuperTrend!.Value;

        bool isUptrend = fast > slow;

        // Entry: Uptrend + Price above both DEMAs + Price above Supertrend
        if (!_inPosition)
        {
            if (isUptrend && close > (decimal)fast && close > (decimal)slow && close > (decimal)super)
            {
                OnBuySignal?.Invoke(klines[last]);
                _inPosition = true;
            }
        }
        else // Exit: Price below slow DEMA OR below Supertrend
        {
            if (close < (decimal)slow || close < (decimal)super)
            {
                OnSellSignal?.Invoke(klines[last]);
                _inPosition = false;
            }
        }
    }

    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}