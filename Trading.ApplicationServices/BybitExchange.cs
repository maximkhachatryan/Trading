using Bybit.Net;
using System.Collections.Concurrent;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoExchange.Net.Authentication;
using Trading.ApplicationContracts;
using Trading.Domain.Constants;
using Trading.Domain.Events;
using Trading.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Trading.ApplicationServices.Configurations;
using OrderSide = Trading.Domain.Enums.OrderSide;

namespace Trading.ApplicationServices;

public class BybitExchange : IExchange
{
    private readonly BybitRestClient _client;
    private readonly BybitSocketClient _socketClient;
    private Action<OrderFilledEvent>? _orderFilledCallback;
    private readonly ConcurrentDictionary<string, (decimal QuantityStep, decimal PriceStep)> _instrumentInfoCache = new();

    public BybitExchange(IOptions<BybitOptions> options)
    {
        var apiCredentials = new ApiCredentials(
            options.Value.ApiKey,
            options.Value.ApiSecret); 

        _client = new BybitRestClient(o =>
        {
            o.ApiCredentials = apiCredentials;
            if (options.Value.UseTestnet)
            {
                o.Environment = BybitEnvironment.Testnet;
            }
        });
        
        _socketClient = new BybitSocketClient(o =>
        {
            o.ApiCredentials = apiCredentials;
            if (options.Value.UseTestnet)
            {
                o.Environment = BybitEnvironment.Testnet;
            }
        });
    }

    public async Task<OrderFilledEvent?> GetFilledOrderById(string orderId)
    {
        var result = await _client.V5Api.Trading.GetOrdersAsync(category: Category.Spot, orderId: orderId);

        if (!result.Success)
        {
            return null;
        }

        var order = result.Data.List.FirstOrDefault();
        if (order == null || order.Status != OrderStatus.Filled)
        {
            return null;
        }

        return new OrderFilledEvent
        {
            OrderId = order.OrderId,
            Symbol = order.Symbol,
            Side = OrderSide.Buy,
            Quantity = order.Quantity,
            ExecutionPrice = order.AveragePrice ?? 0m,
            FilledAt = order.UpdateTime
        };
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

    public async Task<decimal> GetCurrentPrice(string symbol)
    {
        var result = await _client.V5Api.ExchangeData.GetSpotTickersAsync(symbol);
        if (!result.Success || !result.Data.List.Any())
        {
            throw new Exception($"Failed to fetch current price for {symbol}: {result.Error}");
        }

        return result.Data.List.First().LastPrice;
    }

    public async Task<ConditionalOrder> PlaceConditionalOrder(string symbol, OrderSide side, decimal quantity, decimal triggerPrice)
    {
        // Map domain enums to Bybit enums
        var bybitSide = side switch
        {
            OrderSide.Buy => Bybit.Net.Enums.OrderSide.Buy,
            OrderSide.Sell => Bybit.Net.Enums.OrderSide.Sell,
            _ => throw new ArgumentException($"Invalid order side: {side}")
        };

        var (quantityStep, priceStep) = await GetInstrumentInfoAsync(symbol);
        quantity = AdjustValue(quantity, quantityStep);
        triggerPrice = AdjustValue(triggerPrice, priceStep);

        // Place conditional order
        var placeOrderResult = await _client.V5Api.Trading.PlaceOrderAsync(
            category: Category.Spot,
            symbol: symbol,
            side: bybitSide,
            type: NewOrderType.Market,
            marketUnit: MarketUnit.BaseAsset,
            quantity: quantity,
            triggerPrice: triggerPrice,
            orderFilter: OrderFilter.StopOrder);

        if (!placeOrderResult.Success)
        {
            throw new Exception($"Failed to place conditional order: {placeOrderResult.Error}");
        }

        var order = placeOrderResult.Data;
        
        return new ConditionalOrder
        {
            OrderId = order.OrderId,
            Symbol = symbol,
            Quantity = quantity,
            TriggerPrice = triggerPrice,
            PlacedAt = DateTime.UtcNow
        };
    }

    public async Task<OrderFilledEvent?> PlaceMarketOrder(string symbol, OrderSide side, decimal quantity)
    {
        var bybitSide = side switch
        {
            OrderSide.Buy => Bybit.Net.Enums.OrderSide.Buy,
            OrderSide.Sell => Bybit.Net.Enums.OrderSide.Sell,
            _ => throw new ArgumentException($"Invalid order side: {side}")
        };

        var (quantityStep, _) = await GetInstrumentInfoAsync(symbol);
        quantity = AdjustValue(quantity, quantityStep);

        var placeOrderResult = await _client.V5Api.Trading.PlaceOrderAsync(
            category: Category.Spot,
            symbol: symbol,
            side: bybitSide,
            type: NewOrderType.Market,
            marketUnit: MarketUnit.BaseAsset,
            quantity: quantity);

        if (!placeOrderResult.Success)
        {
            throw new Exception($"Failed to place market order: {placeOrderResult.Error}");
        }

        var order = placeOrderResult.Data;
        
        // Market orders might fill immediately, but we might want to wait for the fill event via socket 
        // or fetch it explicitly if we need execution price now.
        // For now, return a partial event or fetch it.
        return await GetFilledOrderById(order.OrderId);
    }

    public async Task<ExchangeSubscriptionResult> SubscribeToOrderUpdates(Action<OrderFilledEvent> onOrderFilled)
    {
        _orderFilledCallback = onOrderFilled;
        var subscriptionResult = await _socketClient.V5PrivateApi.SubscribeToOrderUpdatesAsync(
            update =>
            {
                foreach (var order in update.Data)
                {
                    // Only process filled orders
                    if (order.Status == Bybit.Net.Enums.OrderStatus.Filled)
                    {
                        var side = order.Side == Bybit.Net.Enums.OrderSide.Buy 
                            ? Domain.Enums.OrderSide.Buy 
                            : Domain.Enums.OrderSide.Sell;

                        var filledEvent = new OrderFilledEvent
                        {
                            OrderId = order.OrderId,
                            Symbol = order.Symbol,
                            Side = side,
                            Quantity = order.QuantityFilled!.Value,
                            ExecutionPrice = order.AveragePrice ?? 0,
                            FilledAt = order.UpdateTime
                        };

                        _orderFilledCallback?.Invoke(filledEvent);
                    }
                }
            });

        if (!subscriptionResult.Success)
        {
            throw new Exception($"Failed to subscribe to order updates: {subscriptionResult.Error}");
        }

        var result = new ExchangeSubscriptionResult();

        subscriptionResult.Data.ConnectionLost += result.SendConnectionLost;
        subscriptionResult.Data.ConnectionClosed += result.SendConnectionClosed;
        subscriptionResult.Data.ConnectionRestored += t => result.SendConnectionRestored(t);
        
        return result;
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

    private async Task<(decimal QuantityStep, decimal PriceStep)> GetInstrumentInfoAsync(string symbol)
    {
        if (_instrumentInfoCache.TryGetValue(symbol, out var info))
        {
            return info;
        }

        var result = await _client.V5Api.ExchangeData.GetSpotSymbolsAsync(symbol: symbol);
        if (!result.Success || !result.Data.List.Any())
        {
            throw new Exception($"Failed to fetch instrument info for {symbol}: {result.Error}");
        }

        var instrument = result.Data.List.First();
        var quantityStep = instrument.LotSizeFilter?.BasePrecision ?? 0;
        var priceStep = instrument.PriceFilter?.TickSize ?? 0;

        var newInfo = (quantityStep, priceStep);
        _instrumentInfoCache[symbol] = newInfo;
        return newInfo;
    }

    private decimal AdjustValue(decimal value, decimal step)
    {
        if (step == 0) return value;
        return Math.Floor(value / step) * step;
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