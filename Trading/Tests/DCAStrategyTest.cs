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
            //"APT",
            // "XAUT",
            "BTC",
            //"ETH",
            // "BNB",
            // "SOL",
            // "XRP",
            //"ADA",
            //"DOGE",
            // "AVAX",
            // "DOT",
            // "LINK"
            //"PEPE"
        ];
        foreach (var assetSymbol in assetSymbols)
        {
            var portfolio = new Portfolio("USDT", 100000, assetSymbol);

            var exchange = new BybitExchange();
            var klines = await exchange.GetKlines($"{assetSymbol}USDT", Interval.OneHour, 365 * 24);

            var dcaStrategy = new DCAStrategy(
                sourceSymbol: "USDT",
                assetSymbol: assetSymbol,
                takeProfitRatio: 0.01m,
                priceDeviationRatio: 0.01m,
                tradeValue: 100,
                buyFeePercentage: 0.1m,
                sellFeePercentage: 0.1m);

            dcaStrategy.BackTest(klines, portfolio);
        }
    }
}