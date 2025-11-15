using System.Net.Http.Headers;
using Trading.Abstraction;
using Trading.Base;

namespace Trading.Strategies;

public class DCAStrategy(
    string sourceSymbol,
    string assetSymbol,
    decimal takeProfitPercentage,
    decimal priceDeviationPercentage
    ) : IStrategy
{
    public void Evaluate(Kline[] klines)
    {
        throw new NotImplementedException();
    }

    public void BackTest(Kline[] klines, Portfolio portfolio)
    {
        portfolio.Buy(assetSymbol, klines[0].ClosePrice, 100, 0);
        foreach (var kline in klines.Skip(1))
        {
            var currentPrice = kline.ClosePrice;
            var portfolioAsset = portfolio.Assets[assetSymbol];
            var assetOldPrice = portfolioAsset.AverageBuyPriceIncludingFees;
            if (currentPrice - assetOldPrice > assetOldPrice * takeProfitPercentage / 100)
            {
                portfolio.Sell(assetSymbol, kline.ClosePrice, portfolioAsset.Asset.Balance, 0);
                Console.WriteLine($"Sell \t${portfolioAsset.Asset.Balance * kline.ClosePrice}");
                portfolio.Buy(assetSymbol, kline.ClosePrice, 100, 0);
                Console.WriteLine($"Buy \t$100");
            }
            else if (assetOldPrice - currentPrice > assetOldPrice * priceDeviationPercentage / 100)
            {
                portfolio.Buy(assetSymbol, kline.ClosePrice, 100, 0);
                Console.WriteLine($"Buy \t$100");
            }

        }

        Console.WriteLine($"Finial Portfolio: ${portfolio.CalculateCost()}");
    }

    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}