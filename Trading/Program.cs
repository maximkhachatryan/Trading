using System.Diagnostics;
using System.Runtime.CompilerServices;
using Bybit.Net;
using Bybit.Net.Enums;
using Trading;
using Trading.Base;
using Trading.Constants;
using Trading.Indicators;
using Trading.Strategies;
using BybitExchange = Trading.Exchanges.BybitExchange;

//await RunMySecondStrategy();
//await DoBackTest();
//await CompareStrategies();

//await CompareStrategies();

//await TestEmaMacdRsiAdxTrendStrategy();

await TestSuperTrendStrategy();

// await TestBenoStrategy();

//await TestDemaSuperTrendStrategy();

static async Task TestDemaSuperTrendStrategy()
{
    var exchange = new BybitExchange();
    var klines = await exchange.GetKlines("PEPEUSDT", Interval.FifteenMinutes, 5000);

    var strategyComparer = new StrategyComparer(
        new DemaSupertrendStrategy(
            new DemaIndicator(50),
            new DemaIndicator(200),
            new SupertrendIndicator(12, 6.0)
        ),
        new DemaSupertrendFbbStrategy(
            new DemaIndicator(50),
            new DemaIndicator(200),
            new SupertrendIndicator(12, 6.0),
            new FbbIndicator(20, 1.618, 2.618, 4.236)
        ));

    var result = strategyComparer.Run(klines);

    Console.WriteLine(result);
}


static async Task TestBenoStrategy()
{
    var exchange = new BybitExchange();
    var klines = await exchange.GetKlines("PEPEUSDT", Interval.ThirtyMinutes, 960);

    var strategyComparer = new StrategyComparer(
        new BenoStrategy(
            new RSIIndicator(14),
            new EmaIndicator(14)
        ));

    var result = strategyComparer.Run(klines);

    Console.WriteLine(result);
}

static async Task TestSuperTrendStrategy()
{
    var exchange = new BybitExchange();
    var klines = await exchange.GetKlines("ETHUSDT", Interval.FifteenMinutes, 10000, DateTime.UtcNow.AddDays(-365));

    var strategyComparer = new StrategyComparer(
        new SupertrendStrategy(
            new SupertrendIndicator(10, 3)
        ));

    var result = strategyComparer.Run(klines);

    Console.WriteLine(result);
}

static async Task TestEmaMacdRsiAdxTrendStrategy()
{
    var exchange = new BybitExchange();
    var klines = await exchange.GetKlines("TSLAXUSDT", Interval.OneMinute, 5000);

    var strategyComparer = new StrategyComparer(
        new EmaMacdRsiAdxTrendStrategy(
            new RSIIndicator(14),
            new MacdIndicator(8, 17, 9),
            new EmaIndicator(50),
            new EmaIndicator(200),
            new AdxIndicator(14))
    );

    var result = strategyComparer.Run(klines);

    Console.WriteLine(result);
}

static async Task CompareStrategies()
{
    var exchange = new BybitExchange();
    var klines = await exchange.GetKlines("ETHUSDT", Interval.OneMinute, 2000);

    var strategyComparer = new StrategyComparer(
        EmaMacdRsiAdxTrendStrategy.GetVariants(
            new RSIIndicator(14),
            new MacdIndicator(8, 17, 9),
            new EmaIndicator(50),
            new EmaIndicator(200),
            new AdxIndicator(14)));

    var result = strategyComparer.Run(klines);

    Console.WriteLine(result);
}

static async Task RunMySecondStrategy()
{
    var exchange = new BybitExchange();
    var rsiIndicator = new RSIIndicator(14);
    var macdIndicator = new MacdIndicator(8, 17, 9);
    var ema50Indicator = new EmaIndicator(50);
    var ema200Indicator = new EmaIndicator(200);
    var adxIndicator = new AdxIndicator(14);
    var strategy =
        new EmaMacdRsiAdxTrendStrategy(rsiIndicator, macdIndicator, ema50Indicator, ema200Indicator, adxIndicator);
    strategy.OnBuySignal += OnBuyEventHandler;
    strategy.OnSellSignal += OnSellEventHandler;

    while (true)
    {
        var klines = await exchange.GetKlines("ETHUSDT", Interval.OneMinute, 1000);
        strategy.Evaluate(klines);

        await Task.Delay(60000);
    }
}

static async Task RunMyFirstStrategy()
{
    var exchange = new BybitExchange();
    var rsiIndicator = new RSIIndicator();
    var macdIndicator = new MacdIndicator();
    var strategy = new MacdRsiMomentumStrategy(rsiIndicator, macdIndicator);
    strategy.OnBuySignal += OnBuyEventHandler;
    strategy.OnSellSignal += OnSellEventHandler;

    while (true)
    {
        var klines = await exchange.GetKlines("BTCUSDT", Interval.OneMinute, 1000);
        strategy.Evaluate(klines);

        await Task.Delay(60000);
    }
}

static async Task DoBackTest()
{
    var exchange = new BybitExchange();

    var klines = await exchange.GetKlines("ETHUSDT", Interval.OneMinute, 4000);

    var results = new List<((int RsiPeriod, (int Fast, int Slow, int Signal) MacdParams) Params, decimal Balance)>();
    foreach (var parameters in GetParams())
    {
        var strategy = new EmaMacdRsiAdxTrendStrategy(
            new RSIIndicator(parameters.RsiPeriod),
            new MacdIndicator(parameters.MacdParams.Fast, parameters.MacdParams.Slow, parameters.MacdParams.Signal),
            new EmaIndicator(50),
            new EmaIndicator(200), new AdxIndicator(14));
        var backTester = new BackTester(strategy, initialBalance: 1000);
        backTester.Run(klines);

        results.Add((parameters, backTester.Balance));
    }

    foreach (var result in results.OrderByDescending(r => r.Balance))
    {
        Console.WriteLine($"{result.Params} - {result.Balance}");
    }
}


static IEnumerable<(int RsiPeriod, (int Fast, int Slow, int Signal) MacdParams)> GetParams()
{
    foreach (var rsiPeriod in GetRsiPeriods())
    {
        foreach (var macdComb in GetMacdParamsCombinations())
        {
            yield return (rsiPeriod, macdComb);
        }
    }
}

static IEnumerable<int> GetRsiPeriods()
{
    var rsiPeriods = new int[] { 9, 14, 21 };
    foreach (var period in rsiPeriods)
    {
        yield return period;
    }
}

static IEnumerable<(int Fast, int Slow, int Signal)> GetMacdParamsCombinations()
{
    var macdParamsCombinations = new List<(int Fast, int Slow, int Signal)>
        { (12, 26, 9), (5, 35, 5), (8, 17, 9), (19, 39, 9) };
    foreach (var comb in macdParamsCombinations)
    {
        yield return comb;
    }
}


// var rsi = rsiIndicator.Evaluate(klines);
// var macd = macdIndicator.Evaluate(klines);
//
// foreach (var r in rsi.Where(x => x.Rsi != null).TakeLast(1000))
// {
//     Console.WriteLine($"{r.Date:yyyy-MM-dd HH:mm:ss} - RSI: {r.Rsi:F2}");
// }
//
// //
// //
// foreach (var m in macd.TakeLast(1000))
// {
//     Console.WriteLine($"{m.Date:yyyy-MM-dd HH:mm:ss} - MacdFast: {m.FastEma:F2} - MacdSlow: {m.SlowEma:F2}");
// }

static void OnBuyEventHandler(Kline kline)
{
    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - BUY");
}

static void OnSellEventHandler(Kline kline)
{
    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - SELL");
}