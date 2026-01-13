namespace Trading.ApplicationServices.Configurations;

public class ActivePositionTradingOptions
{
    public const string SectionName = "ActivePositionTrading";
    
    public decimal TradeValue { get; set; } = 1m;
    public decimal TakeProfitPercentage { get; set; } = 1m;
    public decimal PriceDeviationPercentage { get; set; } = 1m;
    public decimal BuyFeePercentage { get; set; } = 1m;
    public decimal SellFeePercentage { get; set; } = 1m;
}
