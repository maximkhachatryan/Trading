using Trading.Base;
using Trading.Constants;
using Trading.Exchanges;
using Trading.Strategies;

namespace Trading.Tests;

public class DCAStrategyTest()
{
    public async Task Test()
    {
        var assetSymbol = "ETH";
        var portfolio = new Portfolio("USDT", 10000, assetSymbol);
        
        var exchange = new BybitExchange();
        var klines = await exchange.GetKlines($"{assetSymbol}USDT", Interval.OneDay, 365);

        var dcaStrategy = new DCAStrategy("USDT", assetSymbol, 20, 10, 100);
        
        dcaStrategy.BackTest(klines, portfolio);
    }
    
    
    
    
    
}