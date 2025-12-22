using Trading.Domain.Enums;

namespace Trading.Domain.ValueObjects;

public record ConditionalOrder
{
    public required string OrderId { get; init; }
    public required string Symbol { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal TriggerPrice { get; init; }
    public required TriggerDirection TriggerDirection { get; init; }
    public required DateTime PlacedAt { get; init; }
}
