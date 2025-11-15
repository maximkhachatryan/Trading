namespace Trading.Base;

public class MultiSymbolKline
{
    public DateTime StartTime { get; set; }
    
    public Dictionary<string, decimal> OpenPrice { get; set; }
    public Dictionary<string, decimal> HighPrice { get; set; }
    public Dictionary<string, decimal> LowPrice { get; set; }
    public Dictionary<string, decimal> ClosePrice { get; set; }
    public Dictionary<string, decimal> Volume { get; set; }
}