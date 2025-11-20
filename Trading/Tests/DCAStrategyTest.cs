using System.Security.Cryptography.X509Certificates;
using Trading.Base;
using Trading.Constants;
using Trading.Exchanges;
using Trading.Strategies;

namespace Trading.Tests;

public class DCAStrategyTest()
{
    public async Task Test()
    {
        string[] assetSymbols =
        [
            // "XAUT",
            "BTC",
            "ETH",
            // "BNB",
            // "SOL",
            // "XRP",
            // "ADA",
            // "DOGE",
            // "AVAX",
            // "DOT",
            // "LINK"
        ];
        foreach (var assetSymbol in assetSymbols)
        {
            var portfolio = new Portfolio("USDT", 1000, assetSymbol);
        
            var exchange = new BybitExchange();
            var klines = await exchange.GetKlines($"{assetSymbol}USDT", Interval.OneHour, 365*24);

            var dcaStrategy = new DCAStrategy("USDT", assetSymbol, 5, 5, 100);
        
            dcaStrategy.BackTest(klines, portfolio);
        }
    }
    
    
    
    
    
}