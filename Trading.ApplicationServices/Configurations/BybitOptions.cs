namespace Trading.ApplicationServices.Configurations;

public class BybitOptions
{
    public const string SectionName = "Bybit";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public bool UseTestnet { get; set; } = false;
}
