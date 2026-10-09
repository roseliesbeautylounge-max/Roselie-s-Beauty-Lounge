using System.Globalization;
using Roselie.Core.Catalog;
using Roselie.Core.Finance;
using Roselie.Core.Models;
using Xunit;

namespace Roselie.Tests;

public sealed class FinancialEngineTests
{
    private static CartLineInput Line(decimal price = 500, decimal quantity = 1, decimal discount = 0,
        SaleItemKind kind = SaleItemKind.Service) => new()
        { Name = "Test line", UnitPrice = price, Quantity = quantity, DiscountPercent = discount, Kind = kind };

    [Fact]
    public void SuppliedHairColorExampleMatchesReceiptToTheCent()
    {
        var input = Line(discount: 10);
        input.Charges =
        [
            new() { Name = "Long Hair", Amount = 300 },
            new() { Name = "Thick Hair", Amount = 200 },
            new() { Name = "Extra Product", Amount = 150 }
        ];
        var result = FinancialEngine.CalculateOrder([input]);
        Assert.Equal(1150m, result.Subtotal);
        Assert.Equal(115m, result.DiscountAmount);
        Assert.Equal(1035m, result.Total);
        Assert.Equal(result.Total, result.Lines.Sum(x => x.Total));
    }

    [Fact]
    public void QuantityMultipliesBaseAndEveryAdditionalCharge()
    {
        var input = Line(quantity: 3, discount: 10);
        input.Charges = [new() { Name = "Charge", Amount = 150 }];
        var result = FinancialEngine.CalculateOrder([input]);
        Assert.Equal(1500m, result.Lines[0].BaseAmount);
        Assert.Equal(450m, result.Lines[0].ChargeAmount);
        Assert.Equal(195m, result.DiscountAmount);
        Assert.Equal(1755m, result.Total);
    }

    [Fact]
    public void DiscountAppliesToEveryChargeAndCanHaveTwoDecimalPlaces()
    {
        var input = Line(100, discount: 12.34m);
        input.Charges = [new() { Name = "Additional", Amount = 0.05m }];
        var result = FinancialEngine.CalculateOrder([input]);
        Assert.Equal(12.35m, result.DiscountAmount);
        Assert.Equal(87.70m, result.Total);
    }

    [Theory]
    [InlineData("0.005", "0.01")]
    [InlineData("1.005", "1.01")]
    [InlineData("-1.005", "-1.01")]
    [InlineData("0.004", "0.00")]
    public void RoundingUsesMidpointsAwayFromZero(string value, string expected)
    {
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture),
            FinancialEngine.RoundMoney(decimal.Parse(value, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void HundredPercentDiscountMakesAValidZeroTotalOrder()
    {
        var result = FinancialEngine.CalculateOrder([Line(599, discount: 100)]);
        Assert.Equal(599m, result.DiscountAmount);
        Assert.Equal(0m, result.Total);
        Assert.Equal(0m, FinancialEngine.CalculatePayment(0, []).BalanceDue);
    }

    [Fact]
    public void MixedItemAndTransactionDiscountsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([Line(discount: 5)], 10));
    }

    [Fact]
    public void FractionalQuantityIsSupportedForRetailStockUnits()
    {
        var input = Line(12.35m, 0.5m, kind: SaleItemKind.Product);
        input.Charges = [new() { Name = "Handling", Amount = 0.03m }];
        var result = FinancialEngine.CalculateOrder([input]);
        Assert.Equal(6.18m, result.Lines[0].BaseAmount);
        Assert.Equal(0.02m, result.Lines[0].ChargeAmount);
        Assert.Equal(6.20m, result.Total);
    }

    [Fact]
    public void FractionalServiceSessionsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([Line(quantity: 0.5m)]));
    }

    [Fact]
    public void OrderDiscountAllocationPreservesCentsAndRefundableLineTotals()
    {
        var result = FinancialEngine.CalculateOrder([Line(0.05m), Line(0.05m), Line(0.05m)], 10);
        Assert.Equal(0.02m, result.DiscountAmount);
        Assert.Equal(new[] { 0.01m, 0.01m, 0m }, result.Lines.Select(x => x.OrderDiscountAmount).ToArray());
        Assert.Equal(0.13m, result.Total);
        Assert.Equal(result.Total, result.Lines.Sum(x => x.Total));
    }

    [Fact]
    public void OrderDiscountAllocationGivesResidualToLargestRemainder()
    {
        Assert.Equal(new[] { 0m, 0.01m }, FinancialEngine.AllocateMoney(0.01m, [1m, 2m]));
    }

    [Fact]
    public void RelativeAllocationWeightsNeedNotRepresentMoneyAmounts()
    {
        Assert.Equal(new[] { 150m, 150m }, FinancialEngine.AllocateMoney(300, [1m, 1m]));
    }

    [Fact]
    public void EmptyCartCannotBeCheckedOut()
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([]));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("500.001")]
    public void InvalidPriceIsRejected(string value)
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([Line(decimal.Parse(value, CultureInfo.InvariantCulture))]));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("100.01")]
    [InlineData("12.345")]
    public void InvalidPercentageIsRejected(string value)
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([Line(discount: decimal.Parse(value, CultureInfo.InvariantCulture))]));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0000001")]
    public void InvalidQuantityIsRejected(string value)
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([
            Line(quantity: decimal.Parse(value, CultureInfo.InvariantCulture), kind: SaleItemKind.Product)]));
    }

    [Fact]
    public void NegativeAdditionalChargeIsRejected()
    {
        var input = Line();
        input.Charges.Add(new() { Name = "Invalid", Amount = -1 });
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([input]));
    }

    [Fact]
    public void CashTenderReturnsExactChange()
    {
        var result = FinancialEngine.CalculatePayment(1035, [new() { Method = PaymentMethod.Cash, Amount = 1500 }]);
        Assert.Equal(1500m, result.AmountReceived);
        Assert.Equal(1035m, result.AppliedAmount);
        Assert.Equal(465m, result.Change);
        Assert.Equal(0m, result.BalanceDue);
    }

    [Fact]
    public void SplitTenderAppliesNoncashFullyAndReturnsOnlyCashChange()
    {
        var result = FinancialEngine.CalculatePayment(1035,
        [
            new() { Method = PaymentMethod.Cash, Amount = 600 },
            new() { Method = PaymentMethod.GCash, Amount = 500, Reference = "Manually recorded" }
        ]);
        Assert.Equal(65m, result.Change);
        Assert.Equal(535m, result.Payments[0].AppliedAmount);
        Assert.Equal(500m, result.Payments[1].AppliedAmount);
        Assert.Equal(1035m, result.AppliedAmount);
    }

    [Fact]
    public void MultipleCashTendersCannotDuplicateAppliedRevenue()
    {
        var result = FinancialEngine.CalculatePayment(100,
        [new() { Method = PaymentMethod.Cash, Amount = 80 }, new() { Method = PaymentMethod.Cash, Amount = 50 }]);
        Assert.Equal(30m, result.Change);
        Assert.Equal(new[] { 80m, 20m }, result.Payments.Select(x => x.AppliedAmount).ToArray());
    }

    [Theory]
    [InlineData(PaymentMethod.GCash)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.Card)]
    public void NoncashOverpaymentIsRejected(PaymentMethod method)
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculatePayment(100, [new() { Method = method, Amount = 101 }]));
    }

    [Fact]
    public void CombinedNoncashOverpaymentIsRejectedEvenWithCashTender()
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculatePayment(100,
        [new() { Method = PaymentMethod.GCash, Amount = 60 }, new() { Method = PaymentMethod.Card, Amount = 50 },
            new() { Method = PaymentMethod.Cash, Amount = 1 }]));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("10.001")]
    public void InvalidPaymentIsRejected(string value)
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculatePayment(10,
            [new() { Method = PaymentMethod.Cash, Amount = decimal.Parse(value, CultureInfo.InvariantCulture) }]));
    }

    [Fact]
    public void PartialTenderShowsBalanceButCannotCompleteCheckout()
    {
        var inputs = new[] { new PaymentInput { Method = PaymentMethod.Cash, Amount = 500 } };
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculatePayment(1035, inputs));
        var partial = FinancialEngine.CalculatePayment(1035, inputs, requireFullPayment: false);
        Assert.Equal(535m, partial.BalanceDue);
        Assert.Equal(0m, partial.Change);
    }

    [Fact]
    public void CumulativePartialRefundsExactlyReconcileAnUnevenTotal()
    {
        var first = FinancialEngine.CalculateRefundAmount(1, 3, 0, 0, 1);
        var second = FinancialEngine.CalculateRefundAmount(1, 3, 1, first, 1);
        var third = FinancialEngine.CalculateRefundAmount(1, 3, 2, first + second, 1);
        Assert.Equal(0.33m, first);
        Assert.Equal(0.34m, second);
        Assert.Equal(0.33m, third);
        Assert.Equal(1m, first + second + third);
    }

    [Fact]
    public void RefundCannotExceedRemainingQuantity()
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateRefundAmount(100, 2, 1, 50, 2));
    }

    [Fact]
    public void RefundCannotAcceptInconsistentPriorRefundHistory()
    {
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateRefundAmount(100, 2, 1, 40, 1));
    }

    [Fact]
    public void DiscountedOrdersReconcileAcrossManyQuantitiesAndPercentages()
    {
        // Financial invariants exercise varied rounding boundaries, rather than repeating engine formulas.
        for (var i = 1; i <= 250; i++)
        {
            var result = FinancialEngine.CalculateOrder([Line(i / 100m, i % 7 + 1), Line((251 - i) / 100m)], i % 101);
            Assert.Equal(result.Subtotal, result.Total + result.DiscountAmount);
            Assert.Equal(result.Total, result.Lines.Sum(x => x.Total));
            Assert.All(result.Lines, x => Assert.True(x.Total >= 0));
            Assert.All(result.Lines, x => Assert.Equal(FinancialEngine.RoundMoney(x.Total), x.Total));
        }
    }

    [Fact]
    public void PackagePurchaseRecognizesRevenueAndRedemptionDoesNot()
    {
        var purchase = FinancialEngine.CalculateOrder([Line(2995, kind: SaleItemKind.Package)]);
        var redemption = FinancialEngine.CalculateOrder([Line(0, kind: SaleItemKind.PackageRedemption)]);
        Assert.Equal(2995m, purchase.Total);
        Assert.Equal(0m, redemption.Total);
        Assert.Throws<ArgumentException>(() => FinancialEngine.CalculateOrder([Line(599, kind: SaleItemKind.PackageRedemption)]));
    }

    [Fact]
    public void CommissionsUseNetRevenueAndFixedRatesFollowQuantity()
    {
        Assert.Equal(103.50m, FinancialEngine.CalculateCommission(CommissionType.Percentage, 10, 1035));
        Assert.Equal(150m, FinancialEngine.CalculateCommission(CommissionType.Fixed, 50, 1035, 3));
        Assert.Equal(0m, FinancialEngine.CalculateCommission(CommissionType.None, 0, 1035));
    }

    [Fact]
    public void WeightedAveragePreservesSixDecimalUnitCostPrecision()
    {
        Assert.Equal(13.333333m, FinancialEngine.CalculateAverageCost(2, 10, 1, 20));
        Assert.Equal(20m, FinancialEngine.CalculateAverageCost(0, 0, 2, 20));
    }

    [Fact]
    public void OfficialCatalogContainsEverySuppliedServiceAndPackage()
    {
        var seed = InitialCatalog.Create();
        Assert.Equal(6, seed.Categories.Count);
        Assert.Equal(40, seed.Services.Count);
        Assert.Equal(21, seed.Packages.Count);
        Assert.All(seed.Packages, x => Assert.Equal(6, x.TotalSessions));
        Assert.All(seed.Services, x => Assert.False(x.OwnerConfirmed));
        Assert.All(seed.Services, x => Assert.False(x.CostInformationComplete));
        Assert.Equal(40, seed.Services.Select(x => x.Id).Distinct().Count());
        var hairColor = Assert.Single(seed.Services, x => x.Name == "Hair Color");
        Assert.Equal(PricingType.Range, hairColor.PricingType);
        Assert.Equal(500m, hairColor.MinimumPrice);
        Assert.Equal(1000m, hairColor.MaximumPrice);
        var tattoo = Assert.Single(seed.Services, x => x.Name == "Tattoo Removal");
        Assert.Equal(PricingType.StartsAt, tattoo.PricingType);
        Assert.Null(tattoo.MaximumPrice);
    }

    [Fact]
    public void CatalogIdsAreStableAcrossApplicationInitializations()
    {
        var first = InitialCatalog.Create();
        var second = InitialCatalog.Create();
        Assert.Equal(first.Services.Select(x => x.Id), second.Services.Select(x => x.Id));
        Assert.Equal(first.Packages.Select(x => x.Id), second.Packages.Select(x => x.Id));
        Assert.Equal(first.Roles.Select(x => x.Id), second.Roles.Select(x => x.Id));
    }
}
