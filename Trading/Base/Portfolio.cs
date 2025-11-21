using Binance.Net.Objects.Models.Spot;
using Trading.Base.Enums;

namespace Trading.Base;

public class Portfolio
{
    private readonly Dictionary<string, PortfolioAsset> _assets = new(StringComparer.OrdinalIgnoreCase);

    public string SourceSymbol { get; }

    public Portfolio(string sourceSymbol, decimal balance, params string[] otherSymbols)
    {
        if (string.IsNullOrWhiteSpace(sourceSymbol))
            throw new ArgumentException("Source symbol cannot be null or empty.", nameof(sourceSymbol));

        if (otherSymbols.Contains(sourceSymbol))
        {
            throw new ArgumentException("Source symbol shouldn't be included in other symbols.", nameof(otherSymbols));
        }

        SourceSymbol = sourceSymbol;
        _assets[sourceSymbol] = new PortfolioAsset
        {
            Symbol = sourceSymbol,
            Balance = balance,
            AveragePrice = 1,
            AveragePriceIncludingFees = 1,
        };

        foreach (var symbol in otherSymbols)
        {
            if (!_assets.ContainsKey(symbol))
            {
                _assets[symbol] = new PortfolioAsset
                {
                    Symbol = symbol,
                    Balance = 0,
                    AveragePrice = 0,
                    AveragePriceIncludingFees = 0
                };
            }
        }
    }

    public IReadOnlyDictionary<string, PortfolioAsset> Assets => _assets.AsReadOnly();

    public decimal CalculateCost(Dictionary<string, decimal> assetPrices)
        => _assets.Values.Sum(x => x.Balance * assetPrices[x.Symbol]);

    public void Buy(DateTime dateTime, string symbol, decimal price, decimal sourceAmount, decimal totalFee = 0)
    {
        var sourceBalanceBefore = _assets[SourceSymbol].Balance;
        var assetAverageBalanceIncludingFeesBefore = _assets[symbol].Balance * _assets[symbol].AveragePriceIncludingFees;
        var assetActualBalanceBefore = _assets[symbol].Balance * price;
        if (sourceBalanceBefore < sourceAmount + totalFee)
        {
            Console.WriteLine("Not enough funds to buy asset");
            return;
        }

        var assetCount = sourceAmount / price;

        var (newAveragePrice, newAveragePriceIncludingFees) =
            PortfolioAsset.CalculatePriceAfterBuying(_assets[symbol], price, sourceAmount, totalFee);

        _assets[symbol].Balance += assetCount;
        _assets[symbol].AveragePrice = newAveragePrice;
        _assets[symbol].AveragePriceIncludingFees = newAveragePriceIncludingFees;

        _assets[SourceSymbol].Balance -= sourceAmount + totalFee;

        Trades.Add(new Trade
        {
            TimeStamp = dateTime,
            ActionType = PortfolioActionType.Buy,
            AssetSymbol = symbol,
            AssetPrice = price,
            AssetsTraded = assetCount,
            Cost = sourceAmount,
            Fee = totalFee,
            SourceBalanceBefore = sourceBalanceBefore,
            AssetAverageBalanceIncludingFeesBefore = assetAverageBalanceIncludingFeesBefore,
            AssetActualBalanceBefore = assetActualBalanceBefore,
            AveragePriceAfter = _assets[symbol].AveragePrice,
            AveragePriceIncludingFees = _assets[symbol].AveragePriceIncludingFees
        });
    }
    
    public void Sell(DateTime dateTime, string symbol, decimal price, decimal sourceAmount, decimal totalFee = 0)
    {
        var assetCount = sourceAmount / price;

        var sourceBalanceBefore = _assets[SourceSymbol].Balance;
        var assetBalanceBefore = _assets[symbol].Balance;
        var assetAverageBalanceIncludingFeesBefore = _assets[symbol].Balance * _assets[symbol].AveragePriceIncludingFees;
        var assetActualBalanceBefore = _assets[symbol].Balance * price;
        
        if (assetBalanceBefore < assetCount)
        {
            Console.WriteLine("Not enough assets to sell");
            return;
        }
        
        var (newAveragePrice, newAveragePriceIncludingFees) =
            PortfolioAsset.CalculatePriceAfterSelling(_assets[symbol], price, sourceAmount, totalFee);

        _assets[symbol].Balance -= assetCount;
        _assets[symbol].AveragePrice = newAveragePrice;
        _assets[symbol].AveragePriceIncludingFees = newAveragePriceIncludingFees;

        _assets[SourceSymbol].Balance += sourceAmount - totalFee;


        Trades.Add(new Trade
        {
            TimeStamp = dateTime,
            ActionType = PortfolioActionType.Sell,
            AssetSymbol = symbol,
            AssetPrice = price,
            AssetsTraded = assetCount,
            Cost = sourceAmount,
            Fee = totalFee,
            SourceBalanceBefore = sourceBalanceBefore,
            AssetAverageBalanceIncludingFeesBefore = assetAverageBalanceIncludingFeesBefore,
            AssetActualBalanceBefore = assetActualBalanceBefore,
            AveragePriceAfter = _assets[symbol].AveragePrice,
            AveragePriceIncludingFees = _assets[symbol].AveragePriceIncludingFees
        });
    }

    public List<Trade> Trades { get; } = [];
}