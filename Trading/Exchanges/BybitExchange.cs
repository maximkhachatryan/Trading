using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoExchange.Net.Authentication;
using Trading.Abstraction;
using Trading.Base;
using Trading.Constants;

namespace Trading.Exchanges;

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


    // public async Task<Kline[]> GetKlines(string symbol, Interval interval, int limit, DateTime? endTime = null)
    // {
    //     var result =
    //         await _client.V5Api.ExchangeData.GetKlinesAsync(Category.Spot, symbol, GetInterval(interval), limit: limit, endTime: endTime);
    //     if (!result.Success)
    //     {
    //         throw new Exception($"Failed to get klines: {result.Error}");
    //     }
    //
    //     return result.Data.List.Select(x => new Kline
    //     {
    //         StartTime = x.StartTime,
    //         ClosePrice = x.ClosePrice,
    //         HighPrice = x.HighPrice,
    //         LowPrice = x.LowPrice,
    //         OpenPrice = x.OpenPrice,
    //         Volume = x.Volume
    //     }).ToArray();
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