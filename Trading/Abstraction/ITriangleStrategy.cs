using Trading.Base;

namespace Trading.Abstraction;

public interface ITriangleStrategy
{
    void Evaluate(KlineTriangle[] klines);
}