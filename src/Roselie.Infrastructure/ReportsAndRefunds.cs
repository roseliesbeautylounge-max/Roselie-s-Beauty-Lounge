using Microsoft.EntityFrameworkCore;
using Roselie.Core.Finance;
using Roselie.Core.Models;
namespace Roselie.Infrastructure;
public sealed partial class BusinessService
{
    public Refund RefundSale(Guid saleId,string reason,PaymentMethod method,UserAccount user,bool isVoid=false,bool returnProductsToStock=true)
    {
        var account=Require(user,true);if(string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A refund or void reason is required.");if(!Enum.IsDefined(method)) throw new ArgumentException("Select a valid refund payment method.");
        lock(_database.Gate)
        {
            using var db=_database.OpenContext();using var transaction=db.Database.BeginTransaction();var sale=db.Set<Sale>().Find(saleId)??throw new ArgumentException("Sale not found.");
            if(sale.Status!=SaleStatus.Completed || db.Set<Refund>().Any(r=>r.SaleId==saleId)) throw new ArgumentException("This transaction has already been refunded or voided.");
            var items=db.Set<SaleItem>().Where(i=>i.SaleId==saleId).ToList();var itemIds=items.Select(i=>i.Id).ToArray();
            var packages=db.Set<CustomerPackage>().Where(p=>itemIds.Contains(p.SaleItemId)).ToList();if(packages.Any(p=>p.UsedSessions>0)) throw new ArgumentException("A package with redeemed sessions cannot be fully refunded. Review the remaining entitlement with the owner.");
            if(items.Any(i=>i.Kind==SaleItemKind.PackageRedemption)) throw new ArgumentException("A completed prepaid session cannot be reversed through a sales refund.");
            var reversibleCommissions=db.Set<CommissionRecord>().Where(c=>c.SaleItemId!=null && itemIds.Contains(c.SaleItemId.Value) && c.PaidAtUtc==null && !c.IsReversed).ToList();
            var refund=new Refund {SaleId=sale.Id,RefundNumber=$"REF-{DateTime.UtcNow.AddHours(8):yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",Amount=sale.Total,CostOfGoodsReversed=returnProductsToStock?sale.CostOfGoods:0,CommissionCostReversed=reversibleCommissions.Sum(c=>c.Amount),Reason=reason.Trim(),Method=method,AuthorizedByUserId=account.Id,IsVoid=isVoid};db.Add(refund);
            foreach(var item in items)
            {
                var returned=item.Kind==SaleItemKind.Product && returnProductsToStock;
                db.Add(new RefundItem {RefundId=refund.Id,SaleItemId=item.Id,Quantity=item.Quantity,Amount=item.Total,ReturnToStock=returned,CostOfGoodsReversed=returned?item.CostOfGoods:0,CommissionCostReversed=reversibleCommissions.Where(c=>c.SaleItemId==item.Id).Sum(c=>c.Amount)});
                if(returned)
                {
                    var product=db.Set<Product>().Find(item.CatalogItemId!.Value)??throw new InvalidOperationException("Original product record is missing.");
                    MoveStock(db,product,item.Quantity,item.UnitCost,InventoryMovementType.Return,"Refund "+refund.RefundNumber,account.Id,item.Id,null,refund.Id,item.CostOfGoods);
                }
            }
            foreach(var package in packages) package.IsCancelled=true;
            foreach(var commission in reversibleCommissions) commission.IsReversed=true;
            sale.Status=isVoid?SaleStatus.Voided:SaleStatus.Refunded;
            AuthService.Audit(db,account.Id,isVoid?"Sale voided":"Sale refunded",sale.Id,$"{refund.RefundNumber}; PHP {refund.Amount:0.00}; {reason}","Sale");db.SaveChanges();transaction.Commit();return refund;
        }
    }
    public FinancialReport GetReport(DateTime fromUtc,DateTime toUtc,UserAccount user)
    {
        Require(user,true);if(fromUtc>=toUtc) throw new ArgumentException("Report end must be after the start. The end date is exclusive.");
        lock(_database.Gate)
        {
            using var db=_database.OpenContext();
            var sales=db.Set<Sale>().AsNoTracking().Where(s=>s.CompletedAtUtc>=fromUtc && s.CompletedAtUtc<toUtc).ToList();var refunds=db.Set<Refund>().AsNoTracking().Where(r=>r.RefundedAtUtc>=fromUtc && r.RefundedAtUtc<toUtc).ToList();
            var expenses=db.Set<Expense>().AsNoTracking().Where(e=>!e.IsVoided && e.OccurredAtUtc>=fromUtc && e.OccurredAtUtc<toUtc).ToList();var redemptions=db.Set<PackageRedemption>().AsNoTracking().Where(r=>r.RedeemedAtUtc>=fromUtc && r.RedeemedAtUtc<toUtc && r.SaleItemId==null).ToList();
            var saleIds=sales.Select(s=>s.Id).ToArray();var items=db.Set<SaleItem>().AsNoTracking().Where(i=>saleIds.Contains(i.SaleId)).ToList();
            var writeoffs=db.Set<InventoryMovement>().AsNoTracking().Where(m=>m.OccurredAtUtc>=fromUtc && m.OccurredAtUtc<toUtc && (m.Type==InventoryMovementType.StockOut || m.Type==InventoryMovementType.Adjustment)).ToList().Where(m=>m.Quantity<0).Sum(m=>-m.ValueChange);
            var report=new FinancialReport {FromUtc=fromUtc,ToUtc=toUtc,GrossSales=sales.Sum(s=>s.Subtotal),Discounts=sales.Sum(s=>s.DiscountAmount),Refunds=refunds.Sum(r=>r.Amount),NetSales=sales.Sum(s=>s.Total)-refunds.Sum(r=>r.Amount),CostOfGoods=sales.Sum(s=>s.CostOfGoods)-refunds.Sum(r=>r.CostOfGoodsReversed),MaterialCost=sales.Sum(s=>s.MaterialCost)+redemptions.Sum(r=>r.MaterialCost)-refunds.Sum(r=>r.MaterialCostReversed),CommissionCost=sales.Sum(s=>s.CommissionCost)+redemptions.Sum(r=>r.CommissionCost)-refunds.Sum(r=>r.CommissionCostReversed),OperatingExpenses=expenses.Where(e=>e.CommissionRecordId==null).Sum(e=>e.Amount),CashCollections=db.Set<Payment>().AsNoTracking().Where(p=>saleIds.Contains(p.SaleId)).ToList().Sum(p=>p.AppliedAmount)-refunds.Sum(r=>r.Amount),TransactionCount=sales.Count,IncompleteCostTransactions=sales.Count(s=>!s.CostInformationComplete)+redemptions.Count(r=>!r.CostInformationComplete)};
            report.DailySales=sales.Select(s=>new ReportValue(s.CompletedAtUtc.AddHours(8).ToString("yyyy-MM-dd"),s.Total)).Concat(refunds.Select(r=>new ReportValue(r.RefundedAtUtc.AddHours(8).ToString("yyyy-MM-dd"),-r.Amount))).GroupBy(v=>v.Label).OrderBy(g=>g.Key).Select(g=>new ReportValue(g.Key,g.Sum(v=>v.Amount))).ToList();
            report.ExpenseBreakdown=expenses.Where(e=>e.CommissionRecordId==null).GroupBy(e=>e.Category).Select(g=>new ReportValue(g.Key,g.Sum(e=>e.Amount))).OrderByDescending(v=>v.Amount).ToList();if(report.CommissionCost!=0) report.ExpenseBreakdown.Add(new ReportValue("Accrued commissions",report.CommissionCost));
            report.InventoryWriteoffCost=writeoffs;report.OperatingExpenses+=writeoffs;if(writeoffs!=0) report.ExpenseBreakdown.Add(new ReportValue("Inventory writeoffs",writeoffs));
            var refundIds=refunds.Select(r=>r.Id).ToArray();var refundedItems=db.Set<RefundItem>().AsNoTracking().Where(r=>refundIds.Contains(r.RefundId)).ToList();var allRefundItemIds=refundedItems.Select(r=>r.SaleItemId).ToArray();var originalRefundItems=db.Set<SaleItem>().AsNoTracking().Where(i=>allRefundItemIds.Contains(i.Id)).ToDictionary(i=>i.Id);
            List<ReportValue> Top(SaleItemKind kind)=>items.Where(i=>i.Kind==kind).Select(i=>new ReportValue(i.Name,i.Total)).Concat(refundedItems.Where(r=>originalRefundItems[r.SaleItemId].Kind==kind).Select(r=>new ReportValue(originalRefundItems[r.SaleItemId].Name,-r.Amount))).GroupBy(v=>v.Label).Select(g=>new ReportValue(g.Key,g.Sum(v=>v.Amount))).OrderByDescending(v=>v.Amount).ToList();
            report.TopServices=Top(SaleItemKind.Service);report.TopProducts=Top(SaleItemKind.Product);return report;
        }
    }
    public ReceiptRecord GetReceipt(Guid saleId,UserAccount user)
    {
        var account=Require(user);lock(_database.Gate) {using var db=_database.OpenContext();var sale=db.Set<Sale>().Find(saleId)??throw new ArgumentException("Sale not found.");if(account.Role!=RoleKind.Administrator && sale.CashierUserId!=account.Id) throw new UnauthorizedAccessException("This receipt belongs to another cashier.");return db.Set<ReceiptRecord>().AsNoTracking().Single(r=>r.SaleId==saleId);}
    }
    public void RecordReceiptPrint(Guid saleId,string printerName,double width,UserAccount user)
    {
        var account=Require(user);if(width is not (57.5 or 58 or 69.5 or 76 or 80)) throw new ArgumentException("Choose a supported receipt paper width: 57.5, 58, 69.5, 76 or 80 mm.");lock(_database.Gate) {using var db=_database.OpenContext();var sale=db.Set<Sale>().Find(saleId)??throw new ArgumentException("Sale not found.");if(account.Role!=RoleKind.Administrator && sale.CashierUserId!=account.Id) throw new UnauthorizedAccessException("This receipt belongs to another cashier.");var receipt=db.Set<ReceiptRecord>().Single(r=>r.SaleId==saleId);if(receipt.PrintCount>0 && account.Role!=RoleKind.Administrator) throw new UnauthorizedAccessException("Administrator access is required to reprint a receipt.");receipt.PrinterName=printerName;receipt.WidthMillimeters=width;receipt.PrintCount++;receipt.LastPrintedAtUtc=DateTime.UtcNow;receipt.LastPrintedByUserId=account.Id;AuthService.Audit(db,account.Id,"Receipt printed",receipt.Id,$"Printer {printerName}; print {receipt.PrintCount}","ReceiptRecord");db.SaveChanges();}
    }
}
