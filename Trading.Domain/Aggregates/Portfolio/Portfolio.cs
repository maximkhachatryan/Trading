using Trading.Domain.Contracts;

namespace Trading.Domain.Aggregates.Portfolio;

public class Portfolio : IAggregateRoot
{
    public int Id { get; set; }
    public string SourceSymbol { get; set; } = null!;
}