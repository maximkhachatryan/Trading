namespace Trading.ApplicationContracts.Dtos.Position;

public record PositionDetailsDto()
{
    public string AssetSymbol { get; set; }
    public string SourceSymbol { get; set; }
    public decimal? AverageNetPrice { get; set; }
    public decimal Cost { get; set; }
    public decimal Quantity { get; set; }
}