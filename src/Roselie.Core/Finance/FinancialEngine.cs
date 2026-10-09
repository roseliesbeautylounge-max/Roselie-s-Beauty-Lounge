using Roselie.Core.Models;

namespace Roselie.Core.Finance;

public sealed class AdditionalChargeInput
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}

public sealed class CartLineInput
{
    public Guid? CatalogItemId { get; set; }
    public Guid? PriceRuleId { get; set; }
    public string Name { get; set; } = "";
    public SaleItemKind Kind { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal DiscountPercent { get; set; }
    public List<AdditionalChargeInput> Charges { get; set; } = [];
    public Guid? EmployeeId { get; set; }
    public Guid? CustomerPackageId { get; set; }
    public Dictionary<Guid, decimal> AdditionalMaterialQuantities { get; set; } = [];
}

public sealed class PaymentInput
{
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = "";
}

public sealed record CalculatedCharge(string Name, string Description, decimal Amount, decimal Total);

public sealed record CalculatedLine(
    Guid? CatalogItemId, string Name, SaleItemKind Kind, decimal UnitPrice, decimal Quantity,
    decimal BaseAmount, IReadOnlyList<CalculatedCharge> Charges, decimal ChargeAmount,
    decimal Subtotal, decimal DiscountPercent, decimal DiscountAmount,
    decimal OrderDiscountAmount, decimal Total, Guid? EmployeeId, Guid? CustomerPackageId);

public sealed record OrderCalculation(IReadOnlyList<CalculatedLine> Lines, decimal Subtotal,
    decimal DiscountPercent, decimal DiscountAmount, decimal Total)
{
    public decimal LineDiscountAmount => Lines.Sum(x => x.DiscountAmount);
    public decimal OrderDiscountAmount => Lines.Sum(x => x.OrderDiscountAmount);
}

public sealed record CalculatedPayment(PaymentMethod Method, decimal Amount, decimal AppliedAmount, string Reference);

public sealed record PaymentCalculation(IReadOnlyList<CalculatedPayment> Payments,
    decimal AmountReceived, decimal AppliedAmount, decimal Change, decimal BalanceDue);

/// <summary>
/// All settlement values are Philippine pesos. Round monetary boundaries to two
/// decimal places, midpoint away from zero. Charges are per unit and inherit the
/// line quantity. Transaction discounts cannot combine with line discounts.
/// Package revenue is recognized once, when sold; prepaid redemptions are zero revenue.
/// </summary>
public static class FinancialEngine
{
    public static decimal RoundMoney(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static OrderCalculation CalculateOrder(IEnumerable<CartLineInput> lines, decimal transactionDiscountPercent = 0)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ValidatePercent(transactionDiscountPercent);
        var inputs = lines.ToList();
        if (inputs.Count == 0) throw new ArgumentException("Add at least one item before checkout.", nameof(lines));
        if (transactionDiscountPercent != 0 && inputs.Any(x => x.DiscountPercent != 0))
            throw new ArgumentException("Use either item discounts or a transaction discount, not both.");

        var calculated = inputs.Select(CalculateLine).ToList();
        var subtotal = calculated.Sum(x => x.Subtotal);
        var lineDiscounts = calculated.Sum(x => x.DiscountAmount);
        var orderDiscount = RoundMoney((subtotal - lineDiscounts) * transactionDiscountPercent / 100m);
        var allocations = AllocateMoney(orderDiscount, calculated.Select(x => x.Total).ToArray());
        for (var i = 0; i < calculated.Count; i++)
            calculated[i] = calculated[i] with
            {
                OrderDiscountAmount = allocations[i],
                Total = calculated[i].Total - allocations[i]
            };
        return new OrderCalculation(calculated.AsReadOnly(), subtotal, transactionDiscountPercent,
            lineDiscounts + orderDiscount, calculated.Sum(x => x.Total));
    }

    public static CalculatedLine CalculateLine(CartLineInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Item name is required.");
        if (!Enum.IsDefined(input.Kind)) throw new ArgumentException("Invalid item type.");
        ValidateMoney(input.UnitPrice, nameof(input.UnitPrice));
        ValidateQuantity(input.Quantity);
        ValidatePercent(input.DiscountPercent);
        if (input.Kind is SaleItemKind.Service or SaleItemKind.Package or SaleItemKind.PackageRedemption &&
            input.Quantity != decimal.Truncate(input.Quantity))
            throw new ArgumentException("Services and packages require a whole-number quantity.");
        if (input.Kind == SaleItemKind.PackageRedemption &&
            (input.UnitPrice != 0 || input.DiscountPercent != 0 || input.Charges.Count != 0))
            throw new ArgumentException("A prepaid session has no sales price, charges, or discount.");

        var baseAmount = RoundMoney(input.UnitPrice * input.Quantity);
        var charges = input.Charges.Select(charge =>
        {
            if (string.IsNullOrWhiteSpace(charge.Name)) throw new ArgumentException("Each charge needs a name.");
            ValidateMoney(charge.Amount, nameof(charge.Amount));
            return new CalculatedCharge(charge.Name.Trim(), charge.Description ?? "", charge.Amount,
                RoundMoney(charge.Amount * input.Quantity));
        }).ToList().AsReadOnly();
        var chargeAmount = charges.Sum(x => x.Total);
        var subtotal = baseAmount + chargeAmount;
        var discount = RoundMoney(subtotal * input.DiscountPercent / 100m);
        return new CalculatedLine(input.CatalogItemId, input.Name.Trim(), input.Kind, input.UnitPrice,
            input.Quantity, baseAmount, charges, chargeAmount, subtotal, input.DiscountPercent,
            discount, 0, subtotal - discount, input.EmployeeId, input.CustomerPackageId);
    }

    public static PaymentCalculation CalculatePayment(decimal total, IEnumerable<PaymentInput> payments,
        bool requireFullPayment = true)
    {
        ValidateMoney(total, nameof(total));
        ArgumentNullException.ThrowIfNull(payments);
        var inputs = payments.ToList();
        foreach (var payment in inputs)
        {
            if (!Enum.IsDefined(payment.Method)) throw new ArgumentException("Invalid payment method.");
            ValidateMoney(payment.Amount, nameof(payment.Amount));
            if (payment.Amount == 0) throw new ArgumentException("Each payment amount must be greater than zero.");
        }
        var nonCash = inputs.Where(x => x.Method != PaymentMethod.Cash).Sum(x => x.Amount);
        if (nonCash > total) throw new ArgumentException("Noncash payments cannot exceed the total due.");
        var received = inputs.Sum(x => x.Amount);
        if (requireFullPayment && received < total) throw new ArgumentException("Payment does not cover the total due.");
        var cashAppliedRemaining = Math.Min(inputs.Where(x => x.Method == PaymentMethod.Cash).Sum(x => x.Amount), total - nonCash);
        var applied = new List<CalculatedPayment>();
        foreach (var payment in inputs)
        {
            var appliedAmount = payment.Method == PaymentMethod.Cash
                ? Math.Min(payment.Amount, cashAppliedRemaining)
                : payment.Amount;
            if (payment.Method == PaymentMethod.Cash) cashAppliedRemaining -= appliedAmount;
            applied.Add(new CalculatedPayment(payment.Method, payment.Amount, appliedAmount, payment.Reference ?? ""));
        }
        var appliedTotal = applied.Sum(x => x.AppliedAmount);
        return new PaymentCalculation(applied.AsReadOnly(), received, appliedTotal,
            Math.Max(0, received - total), total - appliedTotal);
    }

    /// <summary>Allocate exact cents proportionally. Residual cents go to the largest remainder, then line order.</summary>
    public static decimal[] AllocateMoney(decimal amount, IReadOnlyList<decimal> weights)
    {
        ValidateMoney(amount, nameof(amount));
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Any(x => x < 0)) throw new ArgumentException("Allocation weights cannot be negative.");
        var sum = weights.Sum();
        if (amount == 0) return new decimal[weights.Count];
        if (sum <= 0) throw new ArgumentException("An allocation requires a positive total weight.");
        var exactCents = weights.Select(weight => amount * 100m * weight / sum).ToArray();
        var cents = exactCents.Select(decimal.Floor).ToArray();
        var remaining = checked((int)(amount * 100m - cents.Sum()));
        var order = Enumerable.Range(0, weights.Count)
            .OrderByDescending(i => exactCents[i] - cents[i]).ThenBy(i => i).ToArray();
        for (var i = 0; i < remaining; i++) cents[order[i]]++;
        return cents.Select(value => value / 100m).ToArray();
    }

    /// <summary>Cumulative rounding preserves the original discounted sale total across partial refunds.</summary>
    public static decimal CalculateRefundAmount(decimal originalTotal, decimal originalQuantity,
        decimal previouslyRefundedQuantity, decimal previouslyRefundedAmount, decimal refundQuantity)
    {
        ValidateMoney(originalTotal, nameof(originalTotal));
        ValidateMoney(previouslyRefundedAmount, nameof(previouslyRefundedAmount));
        ValidateQuantity(originalQuantity);
        ValidateQuantity(refundQuantity);
        if (previouslyRefundedQuantity < 0 || previouslyRefundedQuantity + refundQuantity > originalQuantity)
            throw new ArgumentException("Refund quantity exceeds the quantity remaining.");
        if (previouslyRefundedQuantity != decimal.Round(previouslyRefundedQuantity, 6))
            throw new ArgumentException("Refund quantities support at most six decimal places.");
        if (previouslyRefundedAmount > originalTotal)
            throw new ArgumentException("Previous refunds exceed the original amount.");
        if (previouslyRefundedAmount != RoundMoney(originalTotal * previouslyRefundedQuantity / originalQuantity))
            throw new ArgumentException("Previous refund amounts do not match the quantity refunded.");
        var cumulative = RoundMoney(originalTotal * (previouslyRefundedQuantity + refundQuantity) / originalQuantity);
        var amount = cumulative - previouslyRefundedAmount;
        if (amount < 0) throw new ArgumentException("Previous refund amounts do not match the quantity refunded.");
        return amount;
    }

    public static decimal CalculateCommission(CommissionType type, decimal rate, decimal netRevenue, decimal quantity = 1)
    {
        if (rate < 0) throw new ArgumentException("Commission rate cannot be negative.");
        ValidateMoney(netRevenue, nameof(netRevenue));
        ValidateQuantity(quantity);
        return type switch
        {
            CommissionType.None => 0,
            CommissionType.Fixed => RoundMoney(rate * quantity),
            CommissionType.Percentage when rate <= 100 => RoundMoney(netRevenue * rate / 100m),
            _ => throw new ArgumentException("Invalid commission type or percentage.")
        };
    }

    public static decimal CalculateAverageCost(decimal currentQuantity, decimal currentAverageCost,
        decimal addedQuantity, decimal addedUnitCost)
    {
        if (currentQuantity < 0 || currentAverageCost < 0 || addedUnitCost < 0)
            throw new ArgumentException("Stock quantities and unit costs cannot be negative.");
        ValidateQuantity(addedQuantity);
        return decimal.Round((currentQuantity * currentAverageCost + addedQuantity * addedUnitCost) /
            (currentQuantity + addedQuantity), 6, MidpointRounding.AwayFromZero);
    }

    public static void ValidateMoney(decimal value, string name = "Amount")
    {
        if (value < 0) throw new ArgumentException($"{name} cannot be negative.");
        if (value != RoundMoney(value)) throw new ArgumentException($"{name} supports at most two decimal places.");
    }

    public static void ValidatePercent(decimal value)
    {
        if (value is < 0 or > 100) throw new ArgumentException("Discount must be between 0% and 100%.");
        if (value != decimal.Round(value, 2)) throw new ArgumentException("Discount supports at most two decimal places.");
    }

    public static void ValidateQuantity(decimal value)
    {
        if (value <= 0 || value != decimal.Round(value, 6))
            throw new ArgumentException("Quantity must be positive with at most six decimal places.");
    }
}
