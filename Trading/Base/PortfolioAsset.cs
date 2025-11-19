using Binance.Net.Objects.Models.Spot;

namespace Trading.Base;

public class PortfolioAsset
{
    public static (decimal AveragePrice, decimal AveragePriceIncludingFees) CalculatePriceAfterBuying(
        PortfolioAsset portfolioAsset, decimal newPrice, decimal buyAmountSource, decimal fee)
    {
        var assetCount = buyAmountSource / newPrice;
        var newAveragePrice =
            (portfolioAsset.AverageBuyPrice * portfolioAsset.Asset.Balance + buyAmountSource) /
            (portfolioAsset.Asset.Balance + assetCount);
        
        var newAveragePriceIncludingFees =
            (portfolioAsset.AverageBuyPriceIncludingFees * portfolioAsset.Asset.Balance + buyAmountSource + fee) /
            (portfolioAsset.Asset.Balance + assetCount);
        
        return(newAveragePrice, newAveragePriceIncludingFees);
    }
    
    public static (decimal AveragePrice, decimal AveragePriceIncludingFees) CalculatePriceAfterSelling(
        PortfolioAsset portfolioAsset, decimal newPrice, decimal sellAmountSource, decimal fee)
    {
        
        var assetCount = sellAmountSource / newPrice;
        
        if (portfolioAsset.Asset.Balance - assetCount <= 0)
            return (AveragePrice: 0, AveragePriceIncludingFees: 0);
        
        var newAveragePrice =
            (portfolioAsset.AverageBuyPrice * portfolioAsset.Asset.Balance - sellAmountSource) /
            (portfolioAsset.Asset.Balance - assetCount);
        
        var newAveragePriceIncludingFees =
            (portfolioAsset.AverageBuyPriceIncludingFees * portfolioAsset.Asset.Balance - sellAmountSource + fee) /
            (portfolioAsset.Asset.Balance - assetCount);

        return (newAveragePrice, newAveragePriceIncludingFees);
    }
    
    public Asset Asset { get; set; } = null!;
    public decimal AverageBuyPrice { get; set; }
    public decimal AverageBuyPriceIncludingFees { get; set; }
    public decimal LastTradePrice { get; set; }
}