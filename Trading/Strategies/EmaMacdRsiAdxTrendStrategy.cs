using Trading.Abstraction;
using Trading.Base;
using Trading.Indicators;

namespace Trading.Strategies;

public class EmaMacdRsiAdxTrendStrategy(
    RSIIndicator rsiIndicator,
    MacdIndicator macdIndicator,
    EmaIndicator ema50,
    EmaIndicator ema200,
    AdxIndicator adxIndicator,
    double rsiEntryMin = 55,
    double rsiEntryMax = 70,
    double rsiExitLow = 45,
    double rsiExitHigh = 75,
    double adxEntryMin = 15,
    double adxExitMin = 20)
    : IStrategy
{
    public static IEnumerable<EmaMacdRsiAdxTrendStrategy> GetVariants(
        RSIIndicator rsiIndicator,
        MacdIndicator macdIndicator, EmaIndicator ema50,
        EmaIndicator ema200,
        AdxIndicator adxIndicator)
    {
        var rsiEntryMinRange = new double[] { 45, 50, 55 };
        var rsiEntryMaxRange = new double[] { 65, 70, 75 };
        var rsiExitLowRange = new double[] { 40, 45, 50 };
        var rsiExitHighRange = new double[] { 70, 75, 80 };
        var adxEntryMinRange = new double[] { 15, 20, 25 };
        var adxExitMinRange = new double[] { 10, 15, 20 };

        foreach (var rsiEntryMin in rsiEntryMinRange)
        foreach (var rsiEntryMax in rsiEntryMaxRange)
        foreach (var rsiExitLow in rsiExitLowRange)
        foreach (var rsiExitHigh in rsiExitHighRange)
        foreach (var adxEntryMin in adxEntryMinRange)
        foreach (var adxExitMin in adxExitMinRange)
        {
            yield return new EmaMacdRsiAdxTrendStrategy(
                rsiIndicator,
                macdIndicator,
                ema50,
                ema200,
                adxIndicator,
                rsiEntryMin,
                rsiEntryMax,
                rsiExitLow,
                rsiExitHigh,
                adxEntryMin,
                adxExitMin);
        }
    }

    private bool _inPosition = false;

    public void Evaluate(Kline[] klines)
    {
        if (klines.Length < 200)
            return;

        var rsi = rsiIndicator.Evaluate(klines);
        var macd = macdIndicator.Evaluate(klines);
        var ema50Result = ema50.Evaluate(klines);
        var ema200Result = ema200.Evaluate(klines);
        var adx = adxIndicator.Evaluate(klines);

        int last = klines.Length - 1;

        if (macd[last].Macd is null || macd[last].Signal is null || rsi[last].Rsi is null ||
            ema50Result[last].Ema is null || ema200Result[last].Ema is null || adx[last].Adx is null)
            return;

        double rsiVal = rsi[last].Rsi.Value;
        double macdVal = macd[last].Macd.Value;
        double macdSig = macd[last].Signal.Value;
        double emaFast = ema50Result[last].Ema.Value;
        double emaSlow = ema200Result[last].Ema.Value;
        double adxVal = adx[last].Adx.Value;

        // Entry
        if (!_inPosition)
        {
            if (emaFast > emaSlow &&
                macdVal > macdSig &&
                rsiVal > rsiEntryMin && rsiVal < rsiEntryMax &&
                adxVal > adxEntryMin)
            {
                OnBuySignal?.Invoke(klines[last]);
                _inPosition = true;
            }
        }
        else // Exit
        {
            if (emaFast < emaSlow ||
                macdVal < macdSig ||
                rsiVal < rsiExitLow || rsiVal > rsiExitHigh ||
                adxVal < adxExitMin)
            {
                OnSellSignal?.Invoke(klines[last]);
                _inPosition = false;
                
                
            }
        }
    }

    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}