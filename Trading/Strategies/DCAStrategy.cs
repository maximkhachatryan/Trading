using System.Net.Http.Headers;
using Skender.Stock.Indicators;
using Trading.Abstraction;
using Trading.Base;

namespace Trading.Strategies;

public class DCAStrategy(
    string sourceSymbol,
    string assetSymbol,
    decimal takeProfitPercentage,
    decimal priceDeviationPercentage,
    decimal tradeValue,
    decimal buyFeePercentage,
    decimal sellFeePercentage
) : IStrategy
{
    public void Evaluate(Kline[] klines)
    {
        throw new NotImplementedException();
    }

    public void BackTest(Kline[] klines, Portfolio portfolio)
    {
        var buyingFee = tradeValue * buyFeePercentage / 100;

        decimal? shortSellTarget = null;
        Console.WriteLine($"StartDate: {klines[0].StartTime}");
        portfolio.Buy(klines[0].StartTime, assetSymbol, klines[0].ClosePrice, tradeValue, tradeValue * buyFeePercentage / 100);
        
        foreach (var kline in klines.Skip(1))
        {
            var currentPrice = kline.ClosePrice;
            var portfolioAsset = portfolio.Assets[assetSymbol];
            var (_, assetPriceAfterBuyingIncludingFees) =
                PortfolioAsset.CalculatePriceAfterBuying(portfolioAsset, currentPrice, tradeValue, buyingFee);

            if (currentPrice - portfolioAsset.AveragePriceIncludingFees >
                portfolioAsset.AveragePriceIncludingFees * takeProfitPercentage / 100)
            {
                portfolio.Sell(kline.StartTime, assetSymbol, kline.ClosePrice,
                    portfolioAsset.Balance * kline.ClosePrice,
                    portfolioAsset.Balance * kline.ClosePrice * sellFeePercentage / 100);
                portfolio.Buy(kline.StartTime, assetSymbol, kline.ClosePrice, tradeValue, buyingFee);
                shortSellTarget = CalculateShortSellTarget(portfolioAsset, currentPrice);
            }
            else if (shortSellTarget != null && currentPrice > shortSellTarget)
            {
                portfolio.Sell(kline.StartTime, assetSymbol, kline.ClosePrice, tradeValue, tradeValue * sellFeePercentage / 100);
                shortSellTarget = CalculateShortSellTarget(portfolioAsset, currentPrice);
            }
            else if (assetPriceAfterBuyingIncludingFees < portfolioAsset.AveragePriceIncludingFees -
                     portfolioAsset.AveragePriceIncludingFees * priceDeviationPercentage / 100)
            {
                portfolio.Buy(kline.StartTime, assetSymbol, currentPrice, tradeValue, buyingFee);

                shortSellTarget = CalculateShortSellTarget(portfolioAsset, currentPrice);
            }
        }

        var assetPrices = new Dictionary<string, decimal>
        {
            { sourceSymbol, 1 },
            { assetSymbol, klines.Last().ClosePrice }
        };
        Console.WriteLine($"Final Portfolio: ${portfolio.CalculateCost(assetPrices)}");
    }

    private decimal? CalculateShortSellTarget(PortfolioAsset portfolioAsset, decimal currentPrice)
    {
        var takeProfitTarget = portfolioAsset.AveragePriceIncludingFees +
                               portfolioAsset.AveragePriceIncludingFees * takeProfitPercentage / 100;
        var depthLevel = portfolioAsset.Cost / tradeValue;

        if (depthLevel <= 2)
        {
            Console.WriteLine(
                $"CurrentPrice: {currentPrice:F2}, DepthLevel:{depthLevel:F2}, ShortSell: null, TakeProfit: {takeProfitTarget:F2}");

            return null;
        }

        var shortSellTarget = currentPrice + currentPrice * takeProfitPercentage / 100;
        Console.WriteLine(
            $"CurrentPrice: {currentPrice:F2}, DepthLevel:{depthLevel:F2}, ShortSell: {shortSellTarget:F2}, TakeProfit: {takeProfitTarget:F2}");

        return shortSellTarget;
    }


    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}