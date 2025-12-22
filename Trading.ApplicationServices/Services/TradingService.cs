using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Services;
using Trading.Domain.Aggregates.Portfolio;
using Trading.Domain.Enums;
using Trading.Domain.Events;

namespace Trading.ApplicationServices.Services;

public class TradingService(
    IExchange exchange,
    decimal takeProfitRatio,
    decimal priceDeviationRatio,
    decimal tradeValue,
    decimal buyFeePercentage,
    decimal sellFeePercentage) : ITradingService
{
    public async Task FireTradingWorker()
    {
        var cancelSucceeded = await exchange.CancelAllUntriggeredConditionalSpotOrder();

        if (!cancelSucceeded)
        {
            return;
        }

        Portfolio portfolio = await GetActivePortfolio();

        // Subscribe to order updates
        await exchange.SubscribeToOrderUpdates(async void (orderFilledEvent) =>
        {
            await HandleOrderFilled(orderFilledEvent, portfolio);
        });

        await PlaceAllConditionalOrders(portfolio);
    }

    private async Task PlaceAllConditionalOrders(Portfolio portfolio, decimal? currentPrice = null)
    {
        var symbol = GetTradingSymbol(portfolio);

        // Place Take Profit order
        var sellPrice = CalculateSellPrice(portfolio);
        var sellQuantity = CalculateSellQuantity(portfolio);
        await exchange.PlaceConditionalOrder(symbol, OrderSide.Sell, sellQuantity, sellPrice, TriggerDirection.Rise);

        // Place Buy on Dip order
        var buyPrice = CalculateBuyPrice(portfolio);
        var buyQuantity = tradeValue / buyPrice;
        await exchange.PlaceConditionalOrder(symbol, OrderSide.Buy, buyQuantity, buyPrice, TriggerDirection.Fall);

        // // Place Short Sell order (if applicable)
        // if (currentPrice.HasValue)
        // {
        //     var shortSellTarget = CalculateShortSellPrice(portfolio, currentPrice.Value);
        //     if (shortSellTarget.HasValue)
        //     {
        //         await exchange.PlaceConditionalOrder(symbol, OrderSide.Sell, tradeValue, shortSellTarget.Value,
        //             TriggerDirection.Rise);
        //     }
        // }
    }

    private async Task HandleOrderFilled(OrderFilledEvent orderFilledEvent, Portfolio portfolio)
    {
        var isSell = orderFilledEvent.Side == OrderSide.Sell;
        //var isShortSelling = IsShortSellingOrder(orderFilledEvent, portfolio);

        await exchange.CancelAllUntriggeredConditionalSpotOrder();

        // // Take Profit: Sell all, buy back initial position, restart
        // if (isSell && !isShortSelling)
        // {
        //     UpdatePortfolio(portfolio, orderFilledEvent, isBuy: false);
        //
        //     // Buy back initial position
        //     portfolio.Buy(
        //         orderFilledEvent.FilledAt,
        //         orderFilledEvent.Symbol,
        //         orderFilledEvent.ExecutionPrice,
        //         tradeValue,
        //         buyFeePercentage);
        //     //TODO: Save portfolio
        //
        //     await PlaceAllConditionalOrders(portfolio);
        //     return;
        // }

        // Buy on Dip or Short Sell: Update portfolio and place all orders
        UpdatePortfolio(portfolio, orderFilledEvent, isBuy: !isSell);
        await PlaceAllConditionalOrders(portfolio, orderFilledEvent.ExecutionPrice);
    }

    // DCA Strategy calculation methods
    private string GetTradingSymbol(Portfolio portfolio) =>"PEPE";

    private PortfolioAsset GetPortfolioAsset(Portfolio portfolio)
    {
        var assetSymbol = GetTradingSymbol(portfolio);
        return portfolio.Assets[assetSymbol];
    }

    private decimal CalculateSellPrice(Portfolio portfolio)
    {
        var portfolioAsset = GetPortfolioAsset(portfolio);
        // Formula to ensure net profit (after selling fee) meets target:
        // NetReceived = sellPrice * (1 - feePercentage)
        // TargetNet = AveragePriceIncludingFees * (1 + takeProfitRatio)
        return (portfolioAsset.AveragePriceIncludingFees * (1 + takeProfitRatio)) / (1 - sellFeePercentage / 100);
    }

    private decimal CalculateSellQuantity(Portfolio portfolio) =>
        GetPortfolioAsset(portfolio).Balance;

    private decimal CalculateBuyPrice(Portfolio portfolio)
    {
        var portfolioAsset = GetPortfolioAsset(portfolio);
        var targetAveragePrice = portfolioAsset.AveragePriceIncludingFees * (1 - priceDeviationRatio);
        var totalFee = tradeValue * buyFeePercentage / 100;

        // Formula derived from:
        // targetAveragePrice = (CurrentCost + tradeValue + totalFee) / (CurrentBalance + tradeValue / buyPrice)
        // targetAveragePrice * (CurrentBalance + tradeValue / buyPrice) = CurrentCost + tradeValue + totalFee
        // targetAveragePrice * CurrentBalance + targetAveragePrice * tradeValue / buyPrice = CurrentCost + tradeValue + totalFee
        // targetAveragePrice * tradeValue / buyPrice = CurrentCost + tradeValue + totalFee - targetAveragePrice * CurrentBalance
        // targetAveragePrice * tradeValue / buyPrice = (CurrentCost - targetAveragePrice * CurrentBalance) + tradeValue + totalFee
        
        // Since targetAveragePrice = CurrentAveragePrice * (1 - priceDeviationRatio)
        // And CurrentCost = CurrentAveragePrice * CurrentBalance
        // CurrentCost - targetAveragePrice * CurrentBalance = CurrentAveragePrice * CurrentBalance - CurrentAveragePrice * (1 - priceDeviationRatio) * CurrentBalance
        // = CurrentAveragePrice * CurrentBalance * (1 - (1 - priceDeviationRatio))
        // = CurrentCost * priceDeviationRatio
        
        // targetAveragePrice * tradeValue / buyPrice = CurrentCost * priceDeviationRatio + tradeValue + totalFee
        // buyPrice = (targetAveragePrice * tradeValue) / (CurrentCost * priceDeviationRatio + tradeValue + totalFee)
        
        var buyPrice = (targetAveragePrice * tradeValue) /
                       (portfolioAsset.Cost * priceDeviationRatio + tradeValue + totalFee);

        return buyPrice;
    }

    // private decimal? CalculateShortSellPrice(Portfolio portfolio, decimal currentPrice)
    // {
    //     var portfolioAsset = GetPortfolioAsset(portfolio);
    //     var depthLevel = portfolioAsset.Cost / tradeValue;
    //
    //     return depthLevel > 2
    //         ? currentPrice * (1 + takeProfitRatio)
    //         : null;
    // }

    // private bool IsShortSellingOrder(OrderFilledEvent orderFilledEvent, Portfolio portfolio)
    // {
    //     var portfolioAsset = GetPortfolioAsset(portfolio);
    //     var orderValue = orderFilledEvent.Quantity * orderFilledEvent.ExecutionPrice;
    //
    //     return Math.Abs(orderValue - tradeValue) < Math.Abs(orderValue - portfolioAsset.Cost);
    // }

    private void UpdatePortfolio(Portfolio portfolio, OrderFilledEvent orderFilledEvent, bool isBuy)
    {
        var orderValue = orderFilledEvent.Quantity * orderFilledEvent.ExecutionPrice;

        if (isBuy)
        {
            portfolio.Buy(
                orderFilledEvent.FilledAt,
                orderFilledEvent.Symbol,
                orderFilledEvent.ExecutionPrice,
                orderValue,
                buyFeePercentage);
        }
        else
        {
            portfolio.Sell(
                orderFilledEvent.FilledAt,
                orderFilledEvent.Symbol,
                orderFilledEvent.ExecutionPrice,
                orderValue,
                sellFeePercentage);
        }

        //TODO: Save portfolio
    }

    private Task<Portfolio> GetActivePortfolio() => Task.FromResult(new Portfolio("USDT", 100000, "PEPE"));
}