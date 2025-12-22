using Trading.Domain.Contracts;
using Trading.Domain.Enums;

namespace Trading.Domain.Aggregates.Portfolio;

public class Portfolio : IAggregateRoot
{
    private readonly Dictionary<string, PortfolioAsset> _assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Trade> _trades = new();

    //public int Id { get; set; }
    public required string SourceSymbol { get; set; }
    public decimal FundedCapital { get; set; }

    // Collections for EF Core
    public ICollection<PortfolioAsset> PortfolioAssets { get; set; } = new List<PortfolioAsset>();
    public ICollection<Trade> PortfolioTrades { get; set; } = new List<Trade>();

    // Read-only access to assets and trades
    public IReadOnlyDictionary<string, PortfolioAsset> Assets => _assets.AsReadOnly();
    public IReadOnlyList<Trade> Trades => _trades.AsReadOnly();

    // Parameterless constructor for EF Core
    private Portfolio() { }

    public Portfolio(string sourceSymbol, decimal balance, params string[] otherSymbols)
    {
        if (string.IsNullOrWhiteSpace(sourceSymbol))
            throw new ArgumentException("Source symbol cannot be null or empty.", nameof(sourceSymbol));

        if (otherSymbols.Contains(sourceSymbol))
            throw new ArgumentException("Source symbol shouldn't be included in other symbols.", nameof(otherSymbols));

        SourceSymbol = sourceSymbol;
        FundedCapital = balance;

        var sourceAsset = new PortfolioAsset
        {
            Symbol = sourceSymbol,
            Balance = balance,
            AveragePrice = 1,
            AveragePriceIncludingFees = 1,
        };
        _assets[sourceSymbol] = sourceAsset;
        PortfolioAssets.Add(sourceAsset);

        foreach (var symbol in otherSymbols)
        {
            if (!_assets.ContainsKey(symbol))
            {
                var asset = new PortfolioAsset
                {
                    Symbol = symbol,
                    Balance = 0,
                    AveragePrice = 0,
                    AveragePriceIncludingFees = 0
                };
                _assets[symbol] = asset;
                PortfolioAssets.Add(asset);
            }
        }
    }

    public decimal CalculateCost(Dictionary<string, decimal> assetPrices)
        => _assets.Values.Sum(x => x.Balance * assetPrices[x.Symbol]);

    public void Buy(DateTime dateTime, string symbol, decimal price, decimal sourceAmount, decimal totalFee = 0)
    {
        if (!_assets.ContainsKey(symbol))
            throw new InvalidOperationException($"Asset {symbol} is not in the portfolio.");

        var sourceBalanceBefore = _assets[SourceSymbol].Balance;
        var assetAverageBalanceIncludingFeesBefore = _assets[symbol].Balance * _assets[symbol].AveragePriceIncludingFees;
        var assetActualBalanceBefore = _assets[symbol].Balance * price;

        if (sourceBalanceBefore < sourceAmount + totalFee)
            throw new InvalidOperationException("Not enough funds to buy asset");

        var assetCount = sourceAmount / price;

        var (newAveragePrice, newAveragePriceIncludingFees) =
            PortfolioAsset.CalculatePriceAfterBuying(_assets[symbol], price, sourceAmount, totalFee);

        _assets[symbol].Balance += assetCount;
        _assets[symbol].AveragePrice = newAveragePrice;
        _assets[symbol].AveragePriceIncludingFees = newAveragePriceIncludingFees;

        _assets[SourceSymbol].Balance -= sourceAmount + totalFee;

        var trade = new Trade
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
        };

        _trades.Add(trade);
        PortfolioTrades.Add(trade);
    }

    public void Sell(DateTime dateTime, string symbol, decimal price, decimal sourceAmount, decimal totalFee = 0)
    {
        if (!_assets.ContainsKey(symbol))
            throw new InvalidOperationException($"Asset {symbol} is not in the portfolio.");

        var assetCount = sourceAmount / price;

        var sourceBalanceBefore = _assets[SourceSymbol].Balance;
        var assetBalanceBefore = _assets[symbol].Balance;
        var assetAverageBalanceIncludingFeesBefore = _assets[symbol].Balance * _assets[symbol].AveragePriceIncludingFees;
        var assetActualBalanceBefore = _assets[symbol].Balance * price;

        if (assetBalanceBefore < assetCount)
            throw new InvalidOperationException("Not enough assets to sell");

        var (newAveragePrice, newAveragePriceIncludingFees) =
            PortfolioAsset.CalculatePriceAfterSelling(_assets[symbol], price, sourceAmount, totalFee);

        _assets[symbol].Balance -= assetCount;
        _assets[symbol].AveragePrice = newAveragePrice;
        _assets[symbol].AveragePriceIncludingFees = newAveragePriceIncludingFees;

        _assets[SourceSymbol].Balance += sourceAmount - totalFee;

        var trade = new Trade
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
        };

        _trades.Add(trade);
        PortfolioTrades.Add(trade);
    }
}