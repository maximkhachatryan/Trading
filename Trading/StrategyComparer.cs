using Trading.Abstraction;
using Trading.Base;

namespace Trading;

public class StrategyComparer(params IEnumerable<IStrategy> strategies)
{
    private decimal _balance;

    private decimal _lastClose;
    private decimal? _positionEntryPrice;
    private Kline? _positionEntryKline;
    private decimal _totalFee;
    
    
    public Dictionary<IStrategy, (decimal BalanceAfter, decimal TotalFee)> Run(Kline[] klines)
    {
        var strategyResults = new Dictionary<IStrategy, (decimal BalanceAfter, decimal TotalFee)>();
        foreach (var strategy in strategies)
        {
            strategy.OnBuySignal += HandleBuy;
            strategy.OnSellSignal += HandleSell;
            _balance = 1000;
            _totalFee = 0;
            
            for (int i = 200; i < klines.Length; i++)
            {
                _lastClose = klines[i].ClosePrice;

                // Pass all klines up to current index
                var history = klines.Take(i + 1).ToArray();
                strategy.Evaluate(history);
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Final Balance: {_balance}");
            strategyResults[strategy] = (_balance, _totalFee);
        }

        return strategyResults;
    }
    
    private void HandleBuy(Kline kline)
    {
        if (_positionEntryPrice == null)
        {
            // Console.ForegroundColor = ConsoleColor.White;
            // Console.WriteLine($"BUY at {_lastClose}");
            _positionEntryPrice = _lastClose;
            _positionEntryKline = kline;
        }
    }

    private void HandleSell(Kline kline)
    {
        if (_positionEntryPrice == null)
        {
            return;
        }

        if (_positionEntryPrice.Value < _lastClose)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        var bet = 1000;
        var fee = bet * 0.2m / 100;
        _totalFee += fee;
        
        Console.WriteLine($"{_positionEntryKline!.StartTime.ToLocalTime()} - {kline.StartTime.ToLocalTime()} Enter {bet}: {_positionEntryPrice.Value}->{_lastClose} \t  {(_lastClose-_positionEntryPrice.Value)/_positionEntryPrice.Value * 100 }%" );
        var profit = (_lastClose - _positionEntryPrice.Value)
            / _positionEntryPrice.Value * bet - fee;

        _balance += profit;
        _positionEntryPrice = null;
        _positionEntryKline = null;
    }
}