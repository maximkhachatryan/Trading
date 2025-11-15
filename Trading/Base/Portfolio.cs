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
            Asset = new Asset { Symbol = sourceSymbol, Balance = balance },
            AverageBuyPrice = 1,
            AverageBuyPriceIncludingFees = 1
        };

        foreach (var symbol in otherSymbols)
        {
            if (!_assets.ContainsKey(symbol))
            {
                _assets[symbol] = new PortfolioAsset
                {
                    Asset = new Asset { Symbol = symbol, Balance = 0 },
                    AverageBuyPrice = 0
                };
            }
        }
    }

    public IReadOnlyDictionary<string, PortfolioAsset> Assets => _assets.AsReadOnly();

    public decimal CalculateCost()
        => _assets.Values.Sum(x => x.Asset.Balance * x.AverageBuyPrice);

    public void Buy(string symbol, decimal price, decimal sourceAmount, decimal totalFee = 0)
    {
        
    }

    public void Sell(string symbol, decimal price, decimal assetAmount, decimal totalFee = 0)
    {
        
    }
}