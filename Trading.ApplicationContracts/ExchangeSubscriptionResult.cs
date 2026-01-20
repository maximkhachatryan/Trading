namespace Trading.ApplicationContracts;

public class ExchangeSubscriptionResult
{
    public void SendConnectionLost() => ConnectionLost?.Invoke();
    public void SendConnectionClosed() => ConnectionClosed?.Invoke();
    
    public void SendConnectionRestored(TimeSpan offlineTimeDuration) => ConnectionRestored?.Invoke(offlineTimeDuration);
    

    public event Action ConnectionLost;
    public event Action ConnectionClosed;
    public event Action<TimeSpan> ConnectionRestored;
    
}