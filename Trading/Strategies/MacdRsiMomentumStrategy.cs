using Trading.Abstraction;
using Trading.Base;
using Trading.Indicators;

namespace Trading.Strategies;

public class MacdRsiMomentumStrategy(RSIIndicator rsiIndicator, MacdIndicator macdIndicator)
    : IStrategy
{
    private bool _inPosition = false;
    
    public void Evaluate(Kline[] klines)
    {
        if (klines.Length < 30) // safety check for indicator warm-up
            return;

        var rsiResults = rsiIndicator.Evaluate(klines);
        var macdResults = macdIndicator.Evaluate(klines);

        // Align lengths
        int lastIndex = Math.Min(rsiResults.Length, macdResults.Length) - 1;
        var lastRsi = rsiResults[lastIndex];
        var lastMacd = macdResults[lastIndex];

        if (lastRsi.Rsi is null || lastMacd.Macd is null || lastMacd.Signal is null)
            return;

        var rsi = lastRsi.Rsi.Value;
        var macd = lastMacd.Macd.Value;
        var signal = lastMacd.Signal.Value;

        if (!_inPosition)
        {
            if (macd > signal && rsi > 50 && rsi < 70)
            {
                OnBuySignal?.Invoke(klines[lastIndex]);
                _inPosition = true;
            }
        }
        else
        {
            if (macd < signal || rsi < 40 || rsi > 70)
            {
                OnSellSignal?.Invoke(klines[lastIndex]);
                _inPosition = false;
            }
        }
    }
    
    
    
    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}