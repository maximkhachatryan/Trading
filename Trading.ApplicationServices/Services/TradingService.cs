using Trading.ApplicationContracts.Services;

namespace Trading.ApplicationServices.Services;

public class TradingService : ITradingService
{
    public TradingService()
    {
        var exchange = new BybitExchange();
    }
}