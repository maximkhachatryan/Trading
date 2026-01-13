using Trading.Domain.Aggregates.Position;
using Trading.Domain.ValueObjects;

namespace Trading.Domain.EventArgs;

public sealed class OrderPlacingEventArgs(IReadOnlyCollection<ConditionalOrderRequest> orderRequest, Position position) : System.EventArgs
{
    public IReadOnlyCollection<ConditionalOrderRequest> OrderRequests { get; private set; } = orderRequest;
    public Position Position { get; private set; } = position;
}