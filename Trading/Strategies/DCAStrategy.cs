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
    decimal tradeValue
) : IStrategy
{
    public void Evaluate(Kline[] klines)
    {
        throw new NotImplementedException();
    }

    public void BackTest(Kline[] klines, Portfolio portfolio)
    {
        decimal buyingFee = tradeValue * 0.02m / 100;


        Console.WriteLine($"StartDate: {klines[0].StartTime}");
        portfolio.Buy(klines[0].StartTime, assetSymbol, klines[0].ClosePrice, tradeValue, tradeValue * 0.02m / 100);
        // Console.WriteLine(
        //     $"Buy  (price = {klines[0].ClosePrice}, avPrIncFees = {portfolio.Assets[assetSymbol].AverageBuyPriceIncludingFees})  \t$100");

        foreach (var kline in klines.Skip(1))
        {
            var currentPrice = kline.ClosePrice;
            var portfolioAsset = portfolio.Assets[assetSymbol];
            var assetOldPrice = portfolioAsset.AverageBuyPriceIncludingFees;
            var (_, assetPriceAfterBuyingIncludingFees) = PortfolioAsset.CalculatePriceAfterBuying(portfolioAsset, currentPrice, tradeValue, buyingFee);
            
            if (currentPrice - portfolioAsset.AverageBuyPriceIncludingFees >
                portfolioAsset.AverageBuyPriceIncludingFees * takeProfitPercentage / 100)
            {
                var assetBalanceBeforeSale = portfolioAsset.Asset.Balance;
                portfolio.Sell(kline.StartTime, assetSymbol, kline.ClosePrice, portfolioAsset.Asset.Balance,
                    portfolioAsset.Asset.Balance * kline.ClosePrice * 0.055m / 100);
                // Console.WriteLine(
                //     $"Sell (price = {kline.ClosePrice}, avPrIncFees = {portfolioAsset.AverageBuyPriceIncludingFees}) \t${assetBalanceBeforeSale * kline.ClosePrice}");

                portfolio.Buy(kline.StartTime, assetSymbol, kline.ClosePrice, tradeValue, buyingFee);
                // Console.WriteLine(
                //     $"Buy  (price = {kline.ClosePrice}, avPrIncFees = {portfolioAsset.AverageBuyPriceIncludingFees})  \t$100");
            }
            // else if (currentPrice - portfolioAsset.LastTradePrice >
            //          portfolioAsset.LastTradePrice * takeProfitPercentage / 3 / 100)
            // {
            //     var assetBalanceBeforeSale = portfolioAsset.Asset.Balance;
            //     var assetsCountToSell = buyAmount / currentPrice;
            //     portfolio.Sell(assetSymbol, kline.ClosePrice, assetsCountToSell,
            //         assetsCountToSell * kline.ClosePrice * 0.055m / 100);
            //     Console.WriteLine(
            //         $"Sell (price = {kline.ClosePrice}, avPrIncFees = {portfolioAsset.AverageBuyPriceIncludingFees}) \t${assetsCountToSell * kline.ClosePrice}");
            //
            // }
            else if (assetPriceAfterBuyingIncludingFees < portfolioAsset.AverageBuyPriceIncludingFees - portfolioAsset.AverageBuyPriceIncludingFees * priceDeviationPercentage / 100)
            {
                portfolio.Buy(kline.StartTime, assetSymbol, kline.ClosePrice, tradeValue, buyingFee);
            }
            
            // else if (currentPrice*portfolioAsset.Asset.Balance 
            //          < assetOldPrice*portfolioAsset.Asset.Balance - 
            //          assetOldPrice*portfolioAsset.Asset.Balance * priceDeviationPercentage / 100)
            // {
            //     portfolio.Buy(kline.StartTime, assetSymbol, kline.ClosePrice, buyAmount, buyingFee);
            //     // Console.WriteLine(
            //     //     $"Buy  (price = {kline.ClosePrice}, avPrIncFees = {portfolioAsset.AverageBuyPriceIncludingFees})  \t$100");
            // }
        }

        var assetPrices = new Dictionary<string, decimal>
        {
            { sourceSymbol, 1},
            { assetSymbol, klines[0].ClosePrice }
        };
        Console.WriteLine($"Final Portfolio: ${portfolio.CalculateCost(assetPrices)}");
    }
    
    
    

    public event Action<Kline>? OnBuySignal;
    public event Action<Kline>? OnSellSignal;
}