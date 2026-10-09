using Roselie.Core.Finance;
using Roselie.Core.Models;
namespace Roselie.Infrastructure;
public sealed class CheckoutRequest
{
    public Guid CheckoutToken { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public List<CartLineInput> Lines { get; set; } = [];
    public List<PaymentInput> Payments { get; set; } = [];
    public decimal TransactionDiscountPercent { get; set; }
    public string DiscountReason { get; set; } = "";
    public string Notes { get; set; } = "";
}
public sealed record ReportValue(string Label, decimal Amount);
public sealed class FinancialReport
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal Refunds { get; set; }
    public decimal NetSales { get; set; }
    public decimal CostOfGoods { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal CommissionCost { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal InventoryWriteoffCost { get; set; }
    public decimal TotalExpenses => OperatingExpenses + CommissionCost;
    public decimal NetProfit => NetSales - CostOfGoods - MaterialCost - TotalExpenses;
    public decimal CashCollections { get; set; }
    public int TransactionCount { get; set; }
    public int IncompleteCostTransactions { get; set; }
    public List<ReportValue> DailySales { get; set; } = [];
    public List<ReportValue> ExpenseBreakdown { get; set; } = [];
    public List<ReportValue> TopServices { get; set; } = [];
    public List<ReportValue> TopProducts { get; set; } = [];
}
