using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoExchange.Net.Authentication;
using Trading.ApplicationContracts;
using Trading.Domain.Constants;
using Trading.Domain.ValueObjects;

namespace Trading.ApplicationServices;

public class BybitExchange : IExchange
{
    private readonly BybitRestClient _client;

    public BybitExchange()
    {
        BybitRestClient.SetDefaultOptions(options =>
        {
            options.ApiCredentials =
                new ApiCredentials("APIKEY",
                    "APISECRET"); // <- Provide you API key/secret in these fields to retrieve data related to your account
        });
        _client = new BybitRestClient();
    }

    public async Task<List<string>> GetUntriggeredConditionalSpotOrderIds(string? symbol = null)
    {
        var result = await _client.V5Api.Trading.GetOrdersAsync(
            symbol: symbol,
            category: Category.Spot,
            orderFilter: OrderFilter.StopOrder
        );

        return result.Data.List.Select(o => o.OrderId).ToList();
    }

    public async Task<bool> CancelAllUntriggeredConditionalSpotOrder(string? symbol = null)
    {
        var result = await _client.V5Api.Trading.CancelAllOrderAsync(symbol: symbol, category: Category.Spot,
            orderFilter: OrderFilter.StopOrder);

        if (result.Success)
        {
            return true;
        }
        
        Console.WriteLine(result.Error?.ToString());
        return false;

    }

    // public async Task Buy(string symbol, decimal qty)
    // {
    //     var placeOrderResult = await _client.V5Api.Trading.PlaceOrderAsync(
    //         category: Category.Spot, symbol: symbol, OrderSide.Buy, NewOrderType.Market, quantity: qty);
    //     if (!placeOrderResult.Success)
    //     {
    //         Console.WriteLine($"Place order failed: {placeOrderResult.Error}");
    //         return;
    //     }
    //     var order = placeOrderResult.Data;
    //     Console.WriteLine($"Order placed — OrderId: {order.OrderId}, ClientOrderId: {order.ClientOrderId}");
    //
    //     
    //     var balanceResult = await _client.V5Api.Account.GetAssetBalanceAsync(AccountType.Spot, symbol);
    //     if (!balanceResult.Success)
    //     {
    //         Console.WriteLine($"Balance fetch failed: {balanceResult.Error}");
    //     }
    //     else
    //     {
    //     }
    // }
    //
    // public async Task BuyConditional(string symbol, decimal qty, decimal triggerPrice)
    // {
    //     var tickerResult = await _client.V5Api.ExchangeData.GetSpotTickersAsync(symbol);
    //     if (!tickerResult.Success || !tickerResult.Data.List.Any())
    //     {
    //         Console.WriteLine($"Ticker fetch failed: {tickerResult.Error}");
    //         return;
    //     }
    //
    //     var currentPrice = tickerResult.Data.List.First().LastPrice;
    //     var triggerDirection = triggerPrice > currentPrice ? TriggerDirection.Rise : TriggerDirection.Fall;
    //
    //     var placeOrderResult = await _client.V5Api.Trading.PlaceOrderAsync(
    //         category: Category.Spot,
    //         symbol: symbol,
    //         side: OrderSide.Buy,
    //         type: NewOrderType.Market,
    //         quantity: qty,
    //         triggerPrice: triggerPrice,
    //         triggerDirection: triggerDirection,
    //         orderFilter: OrderFilter.StopOrder);
    //
    //     if (!placeOrderResult.Success)
    //     {
    //         Console.WriteLine($"Place conditional order failed: {placeOrderResult.Error}");
    //         return;
    //     }
    //
    //     var order = placeOrderResult.Data;
    //     Console.WriteLine($"Conditional Order placed — OrderId: {order.OrderId}, ClientOrderId: {order.ClientOrderId}");
    // }

    public async Task<Kline[]> GetKlines(string symbol, Interval interval, int totalLimit, DateTime? endTime = null)
    {
        const int maxLimitPerRequest = 1000; // Bybit max per request
        var allKlines = new List<Kline>();
        var currentEndTime = endTime ?? DateTime.UtcNow;

        int intervalSeconds = (int)interval; // directly from enum

        while (allKlines.Count < totalLimit)
        {
            int requestLimit = Math.Min(maxLimitPerRequest, totalLimit - allKlines.Count);

            var result = await _client.V5Api.ExchangeData.GetKlinesAsync(
                Category.Spot,
                symbol,
                GetInterval(interval),
                limit: requestLimit,
                endTime: currentEndTime
            );

            if (!result.Success)
                throw new Exception($"Failed to get klines: {result.Error}");

            var klinesBatch = result.Data.List.Select(x => new Kline
            {
                StartTime = x.StartTime,
                OpenPrice = x.OpenPrice,
                HighPrice = x.HighPrice,
                LowPrice = x.LowPrice,
                ClosePrice = x.ClosePrice,
                Volume = x.Volume
            }).ToList();

            if (!klinesBatch.Any())
                break; // No more data

            allKlines.AddRange(klinesBatch);

            // Move endTime backward for next batch
            var earliest = klinesBatch.Min(k => k.StartTime);
            currentEndTime = earliest.AddSeconds(-intervalSeconds);
        }

        // Return candles in chronological order
        return allKlines.OrderBy(k => k.StartTime).ToArray();
    }

    private KlineInterval GetInterval(Interval interval)
    {
        return interval switch
        {
            Interval.OneMinute => KlineInterval.OneMinute,
            Interval.ThreeMinutes => KlineInterval.ThreeMinutes,
            Interval.FiveMinutes => KlineInterval.FiveMinutes,
            Interval.FifteenMinutes => KlineInterval.FifteenMinutes,
            Interval.ThirtyMinutes => KlineInterval.ThirtyMinutes,

            Interval.OneHour => KlineInterval.OneHour,
            Interval.TwoHours => KlineInterval.TwoHours,
            Interval.FourHours => KlineInterval.FourHours,
            Interval.SixHours => KlineInterval.SixHours,
            Interval.TwelveHours => KlineInterval.TwelveHours,

            Interval.OneDay => KlineInterval.OneDay,
            Interval.OneWeek => KlineInterval.OneWeek,
            Interval.OneMonth => KlineInterval.OneMonth,
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }
}