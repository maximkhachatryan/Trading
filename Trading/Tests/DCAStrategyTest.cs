using Trading.Base;
using Trading.Constants;
using Trading.Exchanges;
using Trading.Strategies;

namespace Trading.Tests;

public class DCAStrategyTest()
{
    public async Task Test()
    {
        var portfolio = new Portfolio("USDT", 1000, "ETH");
        
        var exchange = new BybitExchange();
        var klines = await exchange.GetKlines("ETHUSDT", Interval.FifteenMinutes, 5000);

        var dcaStrategy = new DCAStrategy("USDT", "ETH", 30, 20);
        
        dcaStrategy.BackTest(klines, portfolio);
    }
    
    
    
    
    
}