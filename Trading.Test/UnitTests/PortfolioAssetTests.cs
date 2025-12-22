// using Trading.Base;

// namespace Trading.Test.UnitTests;


// public class PortfolioAssetTests
// {
//     private PortfolioAsset CreateAsset(
//         decimal balance,
//         decimal avgPrice = 0,
//         decimal avgPriceWithFees = 0)
//     {
//         return new PortfolioAsset
//         {
//             Symbol = "TEST",
//             Balance = balance,
//             AveragePrice = avgPrice,
//             AveragePriceIncludingFees = avgPriceWithFees
//         };
//     }
//
//     [Test]
//     public void CalculatePriceAfterBuying_ShouldCalculateCorrectAverages()
//     {
//         // Arrange
//         var portfolio = CreateAsset(
//             balance: 10m,
//             avgPrice: 100m,
//             avgPriceWithFees: 105m
//         );
//
//         decimal newPrice = 120m;
//         decimal buyAmountSource = 600m; // you buy 600$ worth of asset
//         decimal fee = 10m;
//
//         // Act
//         var result = PortfolioAsset.CalculatePriceAfterBuying(
//             portfolio, newPrice, buyAmountSource, fee);
//
//         // Assert
//         // assetCount = 600 / 120 = 5
//         // newBalance = 10 + 5 = 15
//
//         // Expected average price:
//         // (100*10 + 600) / 15 = 1600 / 15 = 106.6666666
//         Assert.That(result.AveragePrice, Is.EqualTo(1600m / 15m));
//         // Expected avg including fees:
//         // (105*10 + 600 + 10) / 15
//         // = (1050 + 600 + 10) / 15
//         // = 1660 / 15
//         Assert.That(result.AveragePriceIncludingFees, Is.EqualTo(1660m / 15m));
//     }
// }