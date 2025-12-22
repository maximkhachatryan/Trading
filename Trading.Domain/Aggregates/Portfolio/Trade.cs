using Trading.Domain.Enums;

namespace Trading.Domain.Aggregates.Portfolio;

public class Trade
{
    //public int Id { get; set; }
    //public int PortfolioId { get; set; }
    public DateTime TimeStamp { get; set; }
    public PortfolioActionType ActionType { get; set; }
    public required string AssetSymbol { get; set; }
    public decimal AssetPrice { get; set; }
    public decimal AssetsTraded { get; set; }
    public decimal Cost { get; set; }
    public decimal Fee { get; set; }
    public decimal SourceBalanceBefore { get; set; }
    public decimal AssetAverageBalanceIncludingFeesBefore { get; set; }
    public decimal AssetActualBalanceBefore { get; set; }
    public decimal AveragePriceAfter { get; set; }
    public decimal AveragePriceIncludingFees { get; set; }
    
    // Computed properties
    public decimal CostIncludingFee => Cost + Fee;
    public decimal ActualPortfolioBalanceBefore => SourceBalanceBefore + AssetActualBalanceBefore;

    // Navigation property
    public Portfolio Portfolio { get; set; } = null!;
}