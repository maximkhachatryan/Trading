using System.Drawing;
using Trading.Abstraction;
using Trading.Base;

namespace Trading;

public class BackTester
{
    private readonly IStrategy _strategy;
    private decimal _balance;
    private decimal? _positionEntryPrice;
    private List<(decimal Buy, decimal Sell)> _deals  = new List<(decimal Buy, decimal Sell)>();
    private decimal _lastClose; // keeps track of current kline's close

    public decimal Balance => _balance;

    public BackTester(IStrategy strategy, decimal initialBalance = 1000)
    {
        _strategy = strategy;
        _balance = initialBalance;

        _strategy.OnBuySignal += HandleBuy;
        _strategy.OnSellSignal += HandleSell;
    }

    public void Run(Kline[] klines)
    {
        
        for (int i = 200; i < klines.Length; i++)
        {
            _lastClose = klines[i].ClosePrice;

            // Pass all klines up to current index
            var history = klines.Take(i + 1).ToArray();
            _strategy.Evaluate(history);
        }
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Final Balance: {_balance}");
    }

    private void HandleBuy(Kline kline)
    {
        if (_positionEntryPrice == null)
        {
            // Console.ForegroundColor = ConsoleColor.White;
            // Console.WriteLine($"BUY at {_lastClose}");
            _positionEntryPrice = _lastClose;
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

        var bet = _balance;
        
        Console.WriteLine($"Bet {bet}: {_positionEntryPrice.Value}->{_lastClose} \t  {(_lastClose-_positionEntryPrice.Value)/_positionEntryPrice.Value}%" );
        var profit = (_lastClose - _positionEntryPrice.Value)
            / _positionEntryPrice.Value * bet;

        _deals.Add((_positionEntryPrice.Value, _lastClose));
        _balance += profit;
        _positionEntryPrice = null;
    }
}