namespace Trading.Abstraction;

public interface IIndicator
{
    IEnumerable<IIndicator> GetFamousIndicatorList();
}