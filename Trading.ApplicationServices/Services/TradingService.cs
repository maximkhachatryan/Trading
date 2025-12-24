using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Services;
using Trading.Domain.Aggregates.Portfolio;
using Trading.Domain.Contracts;
using Trading.Domain.Enums;
using Trading.Domain.Events;

namespace Trading.ApplicationServices.Services;

public class TradingService(
    IExchange exchange,
    IPortfolioRepository portfolioRepository,
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

        var portfolio = await portfolioRepository.GetActivePortfolio();
        if (portfolio == null)
            return;

        // Subscribe to order updates
        await exchange.SubscribeToOrderUpdates(async void (orderFilledEvent) =>
        {
            await HandleOrderFilled(orderFilledEvent, portfolio);
        });

        await PlaceAllConditionalOrders(portfolio);
    }
    
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

    private async Task HandleOrderFilled(OrderFilledEvent orderFilledEvent, Portfolio portfolio)
    {
        var isSell = orderFilledEvent.Side == OrderSide.Sell;
        await exchange.CancelAllUntriggeredConditionalSpotOrder(orderFilledEvent.Symbol);
        if (isSell)
        {
            UpdatePortfolio(portfolio, orderFilledEvent, isBuy: false);
        }
        else
        {
            // Buy on Dip or Short Sell: Update portfolio and place all orders
            UpdatePortfolio(portfolio, orderFilledEvent, isBuy: true);
            await PlaceAllConditionalOrders(portfolio);
        }
    }
    
    private async Task PlaceAllConditionalOrders(Portfolio portfolio)
    {
        foreach (var asset in portfolio.Assets.Where(a => a.Key != portfolio.SourceSymbol && a.Value.Cost > 0.001m))
        {
            // Place Take Profit order
            var sellPrice = CalculateSellPrice(asset.Value);
            var sellQuantity = asset.Value.Balance;
            await exchange.PlaceConditionalOrder(asset.Key, OrderSide.Sell, sellQuantity, sellPrice,
                TriggerDirection.Rise);

            // Place Buy on Dip order
            var (buyPrice,_) = CalculateBuyPrice(asset.Value);
            var buyQuantity = tradeValue / buyPrice;
            await exchange.PlaceConditionalOrder(asset.Key, OrderSide.Buy, buyQuantity, buyPrice,
                TriggerDirection.Fall);
            
        }
    }

    private decimal CalculateSellPrice(PortfolioAsset portfolioAsset)
    {
        // Formula to ensure net profit (after selling fee) meets target:
        // NetReceived = sellPrice * (1 - feePercentage)
        // TargetNet = AveragePriceIncludingFees * (1 + takeProfitRatio)
        return (portfolioAsset.AveragePriceIncludingFees * (1 + takeProfitRatio)) / (1 - sellFeePercentage / 100);
    }

    private (decimal BuyPrice, decimal SellPriced) CalculateBuyPrice(PortfolioAsset portfolioAsset)
    {
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

        
        var sellValue = tradeValue * takeProfitRatio;
        var totalSellFee = sellValue * sellFeePercentage / 100;

        // Sell price formula derived from restoring average to pre-buy value
        // Using only post-buy portfolio state (portfolioAsset after buy)
        var sellPrice = (portfolioAsset.AveragePriceIncludingFees * sellValue) /
                        (portfolioAsset.Cost * priceDeviationRatio
                         - portfolioAsset.AveragePriceIncludingFees * portfolioAsset.Balance * priceDeviationRatio
                         + sellValue
                         - totalSellFee);

        return (buyPrice, sellPrice);
    }
}