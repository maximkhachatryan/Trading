using Binance.Net.Clients;
using Binance.Net.Interfaces;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net.Authentication;
using KlineInterval = Bybit.Net.Enums.KlineInterval;

namespace Trading;

public class BinanceService
{
    //private readonly BinanceRestClient _binanceClient;
    private readonly BybitRestClient _client;


    public BinanceService()
    {
        BybitRestClient.SetDefaultOptions(options =>
        {
            options.ApiCredentials = new ApiCredentials("APIKEY", "APISECRET"); // <- Provide you API key/secret in these fields to retrieve data related to your account
        });
        // BinanceRestClient.SetDefaultOptions(options =>
        // {
        //     options.ApiCredentials = new ApiCredentials("APIKEY", "APISECRET"); // <- Provide you API key/secret in these fields to retrieve data related to your account
        // });
        _client = new BybitRestClient();
    }

    public async Task<List<BybitKline>> GetKlines(string symbol = "BTCUSDT", KlineInterval interval = KlineInterval.OneHour, int limit = 100)
    {
        var result = await _client.V5Api.ExchangeData.GetKlinesAsync(Category.Spot, symbol, interval, limit: limit);
        if (!result.Success)
        {
            throw new Exception($"Failed to get klines: {result.Error}");
        }

        return result.Data.List.ToList();
    }
}