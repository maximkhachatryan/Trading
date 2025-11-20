using Trading.Base.Enums;

namespace Trading.Base;

public class Trade
{
    public DateTime TimeStamp { get; set; }
    public PortfolioActionType ActionType { get; set; }
    public string AssetSymbol { get; set; } = null!;
    public decimal AssetPrice { get; set; }
    public decimal AssetsTraded { get; set; }
    public decimal Cost { get; set; }
    public decimal Fee { get; set; }
    public decimal CostIncludingFee => Cost + Fee;
    public decimal SourceBalanceBefore { get; set; }
    public decimal AssetAverageBalanceIncludingFeesBefore { get; set; }
    public decimal AssetActualBalanceBefore { get; set; }
    public decimal ActualPortfolioBalanceBefore => SourceBalanceBefore + AssetActualBalanceBefore;
    public decimal AveragePriceAfter { get; set; }
    public decimal AveragePriceIncludingFees { get; set; }
}