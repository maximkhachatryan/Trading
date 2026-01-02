namespace Trading.Domain.Helpers;

public static class PriceHelper
{
    public static decimal CalculateNetPriceForBuy(decimal grossPrice, decimal feePercentage)
    {
        return 100m * grossPrice / (100m - feePercentage);
    }
    
    public static decimal CalculateGrossPriceForBuy(decimal netPrice, decimal feePercentage)
    {
        return netPrice*(100m-feePercentage)/100m;
    }
    
    public static decimal CalculateNetPriceForSell(decimal grossPrice, decimal feePercentage)
    {
        return grossPrice * (1 - feePercentage / 100m);
    }
    
    public static decimal CalculateGrossPriceForSell(decimal netPrice, decimal feePercentage)
    {
        return netPrice/(1-feePercentage/100m);
    }
}