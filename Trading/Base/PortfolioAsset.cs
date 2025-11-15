namespace Trading.Base;

public class PortfolioAsset
{
    public Asset Asset { get; set; } = null!;
    public decimal AverageBuyPrice { get; set; }
    public decimal TotalFee { get; set; }
}