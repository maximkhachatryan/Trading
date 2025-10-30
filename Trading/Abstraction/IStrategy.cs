using Trading.Base;

namespace Trading.Abstraction;

public interface IStrategy
{
    void Evaluate(Kline[] klines);
    
    event Action<Kline>? OnBuySignal;
    event Action<Kline>? OnSellSignal;

}