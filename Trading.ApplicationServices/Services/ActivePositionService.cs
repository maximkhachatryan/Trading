using Microsoft.Extensions.Options;
using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Dtos.Position;
using Trading.ApplicationContracts.Services;
using Trading.ApplicationServices.Configurations;
using Trading.Domain.Aggregates.Position;
using Trading.Domain.Contracts;
using Trading.Domain.TradingStrategies;

namespace Trading.ApplicationServices.Services;

public class ActivePositionService(
    IActivePositionRepository activePositionRepository,
    IExchange exchange,
    IOptions<ActivePositionTradingOptions> options)
    : IActivePositionService
{
    private readonly ActivePositionTradingOptions _options = options.Value;
    public async Task<bool> OpenPosition(string assetSymbol, string sourceSymbol)
    {
        var position = new Position
        {
            AssetSymbol = assetSymbol,
            SourceSymbol = sourceSymbol
        };
        return await activePositionRepository.TryAdd(position);
    }

    public async Task<bool> ClosePosition(string symbol) //symbol = "ETHUSDT"
    {
        await exchange.CancelAllUntriggeredConditionalSpotOrder(symbol);
        return await activePositionRepository.TryRemove(symbol);
    }

    public async Task<PositionDetailsDto?> GetOpenPosition(string symbol) //symbol = "ETHUSDT"
    {
        var activePosition = await activePositionRepository.GetActivePosition(symbol);

        if (activePosition == null)
        {
            return null;
        }

        var metrics = activePosition.Metrics;

        var strategy = new MainTradingStrategy(
            _options.TradeValue,
            _options.TakeProfitPercentage,
            _options.PriceDeviationPercentage,
            _options.ShortSellDenominator,
            _options.BuyFeePercentage,
            _options.SellFeePercentage);
        var condOrders = strategy.CalculateConditionalOrders(activePosition);
        var currentPrice = await exchange.GetCurrentPrice(activePosition.Symbol);
        
        return new PositionDetailsDto
        {
            AssetSymbol = activePosition.AssetSymbol,
            SourceSymbol = activePosition.SourceSymbol,
            AverageNetPrice = metrics.AverageNetPrice,
            Cost = metrics.Cost,
            Quantity = metrics.Quantity,
            CurrentPrice = currentPrice,
            DipBuyPrice = condOrders?.DipBuyOrder?.TriggerPrice,
            ShortSellPrice = condOrders?.ShortSellOrder?.TriggerPrice,
            FinalSellPrice = condOrders?.FinalSellOrder?.TriggerPrice
        };
    }

    public async Task<Dictionary<string, PositionDetailsDto>> GetOpenPositions()
    {
        var activePositions = await activePositionRepository.GetActivePositions();
        
        var strategy = new MainTradingStrategy(
            _options.TradeValue,
            _options.TakeProfitPercentage,
            _options.PriceDeviationPercentage,
            _options.ShortSellDenominator,
            _options.BuyFeePercentage,
            _options.SellFeePercentage);


        var tasks = activePositions.Select(async kv =>
        {
            var condOrders = strategy.CalculateConditionalOrders(kv.Value);
            var currentPrice = await exchange.GetCurrentPrice(kv.Key);
            return KeyValuePair.Create(kv.Key, new PositionDetailsDto
            {
                AssetSymbol = kv.Value.AssetSymbol,
                SourceSymbol = kv.Value.SourceSymbol,
                AverageNetPrice = kv.Value.Metrics.AverageNetPrice,
                Cost = kv.Value.Metrics.Cost,
                Quantity = kv.Value.Metrics.Quantity,
                CurrentPrice = currentPrice,
                DipBuyPrice = condOrders?.DipBuyOrder?.TriggerPrice,
                ShortSellPrice = condOrders?.ShortSellOrder?.TriggerPrice,
                FinalSellPrice = condOrders?.FinalSellOrder?.TriggerPrice
            });
        });
        
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}