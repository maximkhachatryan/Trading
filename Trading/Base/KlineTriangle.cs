namespace Trading.Base;

public class KlineTriangle
{
    public DateTime StartTime { get; set; }
    
    public decimal AOpenPrice { get; set; }
    public decimal AHighPrice { get; set; }
    public decimal ALowPrice { get; set; }
    public decimal AClosePrice { get; set; }
    public decimal AVolume { get; set; }
    
    public decimal BOpenPrice { get; set; }
    public decimal BHighPrice { get; set; }
    public decimal BLowPrice { get; set; }
    public decimal BClosePrice { get; set; }
    public decimal BVolume { get; set; }
}