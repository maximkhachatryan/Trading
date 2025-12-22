namespace Trading.Domain.Aggregates.Portfolio;

public class PortfolioAsset
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public required string Symbol { get; set; }
    public decimal Balance { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal AveragePriceIncludingFees { get; set; }
    
    public decimal Cost => AveragePriceIncludingFees * Balance;

    // Navigation property
    public Portfolio Portfolio { get; set; } = null!;

    public static (decimal AveragePrice, decimal AveragePriceIncludingFees) CalculatePriceAfterBuying(
        PortfolioAsset portfolioAsset, decimal newPrice, decimal buyAmountSource, decimal fee)
    {
        var assetCount = buyAmountSource / newPrice;
        var newAveragePrice =
            (portfolioAsset.AveragePrice * portfolioAsset.Balance + buyAmountSource) /
            (portfolioAsset.Balance + assetCount);
        
        var newAveragePriceIncludingFees =
            (portfolioAsset.AveragePriceIncludingFees * portfolioAsset.Balance + buyAmountSource + fee) /
            (portfolioAsset.Balance + assetCount);
        
        return (newAveragePrice, newAveragePriceIncludingFees);
    }
    
    public static (decimal AveragePrice, decimal AveragePriceIncludingFees) CalculatePriceAfterSelling(
        PortfolioAsset portfolioAsset, decimal newPrice, decimal sellAmountSource, decimal fee)
    {
        var assetCount = sellAmountSource / newPrice;
        
        if (portfolioAsset.Balance - assetCount <= 0.00000001m)
            return (AveragePrice: 0, AveragePriceIncludingFees: 0);
        
        var newAveragePrice =
            (portfolioAsset.AveragePrice * portfolioAsset.Balance - sellAmountSource) /
            (portfolioAsset.Balance - assetCount);
        
        var newAveragePriceIncludingFees =
            (portfolioAsset.AveragePriceIncludingFees * portfolioAsset.Balance - sellAmountSource + fee) /
            (portfolioAsset.Balance - assetCount);

        return (newAveragePrice, newAveragePriceIncludingFees);
    }
}