namespace Trading.ApplicationContracts.Services;

public interface IActivePositionTradingService
{
    Task<bool> StartTrading();
    Task ResetConditionalOrders();
}