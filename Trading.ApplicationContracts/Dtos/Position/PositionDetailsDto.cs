namespace Trading.ApplicationContracts.Dtos.Position;

public record PositionDetailsDto()
{
    public string AssetSymbol { get; init; } = null!;
    public string SourceSymbol { get; init; } = null!;
    public decimal? AverageNetPrice { get; init; }
    public decimal Cost { get; init; }
    public decimal Quantity { get; init; }
    public decimal? DipBuyPrice { get; init; }
    public decimal? ShortSellPrice { get; init; }
    public decimal? FinalSellPrice { get; init; }
    public decimal CurrentPrice { get; init; }
}