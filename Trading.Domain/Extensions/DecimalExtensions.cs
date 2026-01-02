namespace Trading.Domain.Extensions;

public static class DecimalExtensions
{
    public static decimal IncreaseByPercentage(this decimal value, decimal percentage)
    {
        return value * (1 + percentage / 100m);
    }
    
    public static decimal DecreaseByPercentage(this decimal value, decimal percentage)
    {
        return value * (1 - percentage / 100m);
    }
}