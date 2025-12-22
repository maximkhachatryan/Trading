using Trading.ApplicationContracts;
using Trading.ApplicationContracts.Services;

namespace Trading.ApplicationServices.Services;

public class TradingService(IExchange exchange) : ITradingService
{
    public async Task FireTradingWorker()
    {
        var cancelSucceeded = await exchange.CancelAllUntriggeredConditionalSpotOrder();

        if (!cancelSucceeded)
        {
            return;
        }
        
        // Get Active Portfolio
        
        // Place Conditional Spot Order for selling
        
        // Place Conditional Spot Order for buying
        
        // Listen to order topic
        
        // - if (order is Filled && order.Side == Sell && order is not short selling)
        
        // -- then CancelAllOrders
        // -- close Portfolio and exit
        
        
        // - if (order is Filled && order.Side == Buy)
        
        // -- then CancelAllOrders
        // -- Update Portfolio
        // -- Place Conditional Spot Order for selling
        // -- Place Conditional Spot Order for buying
        // -- Place Conditional Spot Order for Short Selling
        
        // - if (order is Filled && order.Side == Sell && order is short selling)
        
        // -- then CancelAllOrders
        // -- Update Portfolio
        // -- Place Conditional Spot Order for selling
        // -- Place Conditional Spot Order for buying
        // -- Place Conditional Spot Order for Short Selling
        
        
    }
}