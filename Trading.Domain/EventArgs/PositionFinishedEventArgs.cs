using Trading.Domain.Aggregates.Position;

namespace Trading.Domain.EventArgs;

public sealed class PositionFinishedEventArgs(Position position) : System.EventArgs
{
    public Position Position { get; private set; } = position;
}