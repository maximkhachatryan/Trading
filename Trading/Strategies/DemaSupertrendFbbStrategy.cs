using Trading.Abstraction;
using Trading.Base;
using Trading.Indicators;

namespace Trading.Strategies;

public class DemaSupertrendFbbStrategy(
    DemaIndicator fastDema,
    DemaIndicator slowDema,
    SupertrendIndicator supertrend,
    FbbIndicator fbb)
    : IStrategy
{
    private bool _inPosition = false;

    public void Evaluate(Kline[] klines)
    {
        if (klines.Length < new[] { fastDema.Period, slowDema.Period, supertrend.AtrPeriod, fbb.Period }.Max())
            return;

        var fast = fastDema.Evaluate(klines);
        var slow = slowDema.Evaluate(klines);
        var stResults = supertrend.Evaluate(klines);
        var fbbResults = fbb.Evaluate(klines);

        int last = klines.Length - 1;

        // Validate results
        if (fast[last].Dema is null ||
            slow[last].Dema is null ||
            stResults[last].SuperTrend is null ||
            fbbResults[last].Bands.Count == 0)
            return;

        decimal close = klines[last].ClosePrice;
        double fastVal = fast[last].Dema.Value;
        double slowVal = slow[last].Dema.Value;
        double super = (double)stResults[last].SuperTrend.Value;

        bool isUptrend = fastVal > slowVal;

        // Use 1.618 Fibonacci band for entry filter
        var fibEntry = fbbResults[last].Bands.ContainsKey(1.618) ? fbbResults[last].Bands[1.618].Upper : null;

        if (!_inPosition)
        {
            // Long entry: Uptrend + above Supertrend + above FBB 1.618
            if (isUptrend && close > (decimal)super && fibEntry.HasValue && close > fibEntry.Value)
            {
                OnBuySignal?.Invoke(klines[last]);
                _inPosition = true;
            }
            // Short entry: Downtrend + below Supertrend + below FBB 1.618 lower
            else if (!isUptrend && close < (decimal)super && fbbResults[last].Bands.ContainsKey(1.618))
            {
                var fibLower = fbbResults[last].Bands[1.618].Lower;
                if (fibLower.HasValue && close < fibLower.Value)
                {
                    OnSellSignal?.Invoke(klines[last]);
                    _inPosition = true;
                }
            }
        }
        else
        {
            // Exit conditions
            if (isUptrend)
            {
                // Exit long if price drops below slow DEMA or Supertrend
                if (close < (decimal)slowVal || close < (decimal)super)
                {
                    OnSellSignal?.Invoke(klines[last]);
                    _inPosition = false;
                }
            }
            else
            {
                // Exit short if price rises above slow DEMA or Supertrend
                if (close > (decimal)slowVal || close > (decimal)super)
                {
                    OnBuySignal?.Invoke(klines[last]);
                    _inPosition = false;
                }
            }
        }
    }

    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}