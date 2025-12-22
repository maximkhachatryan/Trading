namespace Trading.Domain.Events;

public record OrderFilledEvent
{
    public required string OrderId { get; init; }
    public required string Symbol { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal ExecutionPrice { get; init; }
    public required DateTime FilledAt { get; init; }
}
