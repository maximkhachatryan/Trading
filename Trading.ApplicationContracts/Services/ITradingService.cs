using System.Threading.Tasks;

namespace Trading.ApplicationContracts.Services;

public interface ITradingService
{
    Task FireTradingWorker();
}