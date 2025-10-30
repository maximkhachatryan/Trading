using Trading.Abstraction;
using Trading.Base;

namespace Trading.Strategies;

public class TriangleStrategy : ITriangleStrategy
{
    private decimal _AOldPrice;
    private decimal _BOldPrice;
    private readonly decimal _APreferedWeight;//10
    private readonly decimal _BPreferedWeight;//10

    public TriangleStrategy(decimal priceA, decimal priceB, decimal preferedWaightA, decimal preferedWaightB)
    {
        _AOldPrice = priceA;
        _BOldPrice = priceB;
        _APreferedWeight = preferedWaightA;
        _BPreferedWeight = preferedWaightB;
    }

    public void Evaluate(KlineTriangle[] klines)
    {
        foreach (var kline in klines)
        {
            var aWeightGrowth = _APreferedWeight * (kline.AClosePrice - _AOldPrice) / 100;
            var bWeightGrowth = _BPreferedWeight * (kline.BClosePrice - _BOldPrice) / 100;

            if (_AOldPrice / kline.AClosePrice > 1.01m)
            {
                
            }
            else if (_AOldPrice / kline.AClosePrice < 0.99m)
            {
                
            }
        }
    }
}