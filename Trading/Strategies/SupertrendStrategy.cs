using Trading.Abstraction;
using Trading.Base;
using Trading.Indicators;

namespace Trading.Strategies;

public class SupertrendStrategy(
    SupertrendIndicator supertrendIncicator,
    int atrPeriod = 10,
    double multiplier = 3.0)
    : IStrategy
{
    public static IEnumerable<SupertrendStrategy> GetVariants(SupertrendIndicator supertrendIncicator)
    {
        var atrPeriods = new int[] { 7, 10, 14 };
        var multipliers = new double[] { 2.0, 3.0, 4.0 };

        foreach (var atr in atrPeriods)
        foreach (var mult in multipliers)
        {
            yield return new SupertrendStrategy(supertrendIncicator, atr, mult);
        }
    }

    private bool _inPosition = false;

    public void Evaluate(Kline[] klines)
    {
        if (klines.Length < atrPeriod)
            return;

        var results = supertrendIncicator.Evaluate(klines);
        int last = klines.Length - 1;

        var close = (decimal)klines[last].ClosePrice;
        var super = results[last].SuperTrend;

        if (super is null)
            return;

        // Entry
        if (!_inPosition)
        {
            // Buy when price closes above SuperTrend line
            if (close > super.Value)
            {
                OnBuySignal?.Invoke(klines[last]);
                _inPosition = true;
            }
        }
        else // Exit
        {
            // Sell when price closes below SuperTrend line
            if (close < super.Value)
            {
                OnSellSignal?.Invoke(klines[last]);
                _inPosition = false;
            }
        }
    }

    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}