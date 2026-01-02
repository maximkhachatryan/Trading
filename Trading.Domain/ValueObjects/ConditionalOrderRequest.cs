using Trading.Domain.Enums;

namespace Trading.Domain.ValueObjects;

public record ConditionalOrderRequest
{
    public required string Symbol { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal TriggerPrice { get; init; }
    public required TriggerDirection TriggerDirection { get; init; }
}