namespace Trading.ApplicationContracts.Services;

public interface INotifier
{
    Task Notify(string message);
}
