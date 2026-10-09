using System.Text;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Finance;
using Roselie.Core.Models;

namespace Roselie.Infrastructure;

public sealed partial class BusinessService
{
    public Sale Checkout(CheckoutRequest request,UserAccount user)
    {
        var account=Require(user);if(account.Role==RoleKind.SalonStaff) throw new UnauthorizedAccessException("Cashier or administrator access is required for checkout.");
        if(request.CheckoutToken==Guid.Empty) throw new ArgumentException("A checkout request token is required.");
        lock(_database.Gate)
        {
            using var db=_database.OpenContext();using var transaction=db.Database.BeginTransaction();
            var prior=db.Set<Sale>().AsNoTracking().SingleOrDefault(s=>s.CheckoutToken==request.CheckoutToken);
            if(prior!=null) {if(prior.CashierUserId!=account.Id) throw new UnauthorizedAccessException("This checkout belongs to another cashier.");return prior;}
            if(request.CustomerId.HasValue && !db.Set<Customer>().Any(c=>c.Id==request.CustomerId && c.IsActive)) throw new ArgumentException("Select an active customer.");
            FinancialEngine.ValidatePercent(request.TransactionDiscountPercent);if(request.TransactionDiscountPercent>account.MaxDiscountPercent) throw new UnauthorizedAccessException("Transaction discount exceeds this account's authorized limit.");
            foreach(var input in request.Lines) ValidateCartLine(db,input,request.CustomerId,account);
            var calculation=FinancialEngine.CalculateOrder(request.Lines,request.TransactionDiscountPercent);
            var payment=FinancialEngine.CalculatePayment(calculation.Total,request.Payments);
            var sale=new Sale { CheckoutToken=request.CheckoutToken,TransactionNumber=$"RBL-{DateTime.UtcNow.AddHours(8):yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",CustomerId=request.CustomerId,CashierUserId=account.Id,Subtotal=calculation.Subtotal,DiscountPercent=calculation.DiscountPercent,DiscountAmount=calculation.DiscountAmount,Total=calculation.Total,AmountReceived=payment.AmountReceived,Change=payment.Change,DiscountReason=request.DiscountReason,Notes=request.Notes };
            db.Add(sale);var saleItems=new List<SaleItem>();
            for(var i=0;i<calculation.Lines.Count;i++)
            {
                var line=calculation.Lines[i];var input=request.Lines[i];
                var item=new SaleItem {SaleId=sale.Id,CatalogItemId=line.CatalogItemId,PriceRuleId=input.PriceRuleId,Kind=line.Kind,Name=line.Name,Quantity=line.Quantity,UnitPrice=line.UnitPrice,BaseAmount=line.BaseAmount,ChargeAmount=line.ChargeAmount,Subtotal=line.Subtotal,DiscountPercent=line.DiscountPercent,DiscountAmount=line.DiscountAmount,OrderDiscountAmount=line.OrderDiscountAmount,Total=line.Total,EmployeeId=line.EmployeeId,CustomerPackageId=line.CustomerPackageId};
                db.Add(item);saleItems.Add(item);
                if(input.PriceRuleId.HasValue)
                {
                    var priceRule=db.Set<ServicePriceRule>().Find(input.PriceRuleId.Value)!;
                    AuthService.Audit(db,account.Id,"Service price rule applied",item.Id,$"{priceRule.Name}; agreed unit price PHP {item.UnitPrice:0.00}","SaleItem");
                }
                foreach(var charge in line.Charges) db.Add(new SaleItemCharge {SaleItemId=item.Id,Name=charge.Name,Description=charge.Description,Amount=charge.Amount,Total=charge.Total});
                if(item.DiscountAmount>0) db.Add(new SaleItemDiscount{SaleItemId=item.Id,Percent=item.DiscountPercent,Amount=item.DiscountAmount,Reason=request.DiscountReason,AuthorizedByUserId=account.Id});
                if(item.OrderDiscountAmount>0) db.Add(new SaleItemDiscount{SaleItemId=item.Id,Percent=sale.DiscountPercent,Amount=item.OrderDiscountAmount,Reason=request.DiscountReason,AuthorizedByUserId=account.Id,IsOrderAllocation=true});
                switch(item.Kind)
                {
                    case SaleItemKind.Product:
                        var product=db.Set<Product>().Find(item.CatalogItemId!.Value)!;item.Name=product.Name;item.UnitCost=product.AverageCost;item.CostInformationComplete=product.CostInformationComplete;
                        item.CostOfGoods=MoveStock(db,product,-item.Quantity,product.AverageCost,InventoryMovementType.RetailSale,"Retail sale",account.Id,item.Id);break;
                    case SaleItemKind.Service:
                        var service=db.Set<SalonService>().Find(item.CatalogItemId!.Value)!;item.Name=service.Name;
                        var consumed=ConsumeMaterials(db,service,item.Quantity,input.AdditionalMaterialQuantities,account.Id,item.Id,null);item.MaterialCost=consumed.Cost;item.CostInformationComplete=consumed.Complete;
                        item.CommissionCost=CreateCommission(db,item.EmployeeId,service.Id,item.Total,item.Quantity,account.Id,item.Id,null);break;
                    case SaleItemKind.Package:
                        var offer=db.Set<PackageOffer>().Find(item.CatalogItemId!.Value)!;var packageService=db.Set<SalonService>().Find(offer.ServiceId)!;item.Name=offer.Name;item.CostInformationComplete=packageService.CostInformationComplete;
                        var allocations=FinancialEngine.AllocateMoney(item.Total,Enumerable.Repeat(item.Total/item.Quantity,checked((int)item.Quantity)).ToArray());
                        for(var n=0;n<checked((int)item.Quantity);n++) db.Add(new CustomerPackage {CustomerId=request.CustomerId!.Value,PackageOfferId=offer.Id,ServiceId=offer.ServiceId,SaleItemId=item.Id,Name=offer.Name,PurchasePrice=allocations[n],TotalSessions=offer.TotalSessions,ExpiresAtUtc=offer.ValidityDays.HasValue ? DateTime.UtcNow.AddDays(offer.ValidityDays.Value) : null});
                        break;
                    case SaleItemKind.PackageRedemption:
                        var redemption=RedeemWithinTransaction(db,input.CustomerPackageId!.Value,input.EmployeeId,account,item.Id,input.AdditionalMaterialQuantities);item.MaterialCost=redemption.MaterialCost;item.CommissionCost=redemption.CommissionCost;item.CostInformationComplete=redemption.CostInformationComplete;break;
                }
                sale.CostOfGoods+=item.CostOfGoods; sale.MaterialCost+=item.MaterialCost; sale.CommissionCost+=item.CommissionCost; sale.CostInformationComplete &= item.CostInformationComplete;
            }
            foreach(var p in payment.Payments) db.Add(new Payment {SaleId=sale.Id,Method=p.Method,Amount=p.Amount,AppliedAmount=p.AppliedAmount,Reference=p.Reference});
            db.Add(new ReceiptRecord {SaleId=sale.Id,ReceiptNumber=sale.TransactionNumber,Content=BuildReceipt(db,sale,saleItems,calculation,payment,account)});
            AuthService.Audit(db,account.Id,"Checkout completed",sale.Id,$"{sale.TransactionNumber}; Total PHP {sale.Total:0.00}; Discount PHP {sale.DiscountAmount:0.00}","Sale");
            db.SaveChanges();transaction.Commit();return sale;
        }
    }
    private static void ValidateCartLine(SalonDbContext db,CartLineInput line,Guid? customerId,UserAccount user)
    {
        if(line.PriceRuleId.HasValue && line.Kind!=SaleItemKind.Service) throw new ArgumentException("A service price rule applies to a service only.");
        if(line.Kind is not (SaleItemKind.Service or SaleItemKind.PackageRedemption) && line.AdditionalMaterialQuantities.Count>0) throw new ArgumentException("Additional material quantities apply to performed services only.");
        if(line.DiscountPercent>user.MaxDiscountPercent) throw new UnauthorizedAccessException("Item discount exceeds this account's authorized limit.");
        if(!line.CatalogItemId.HasValue && line.Kind!=SaleItemKind.PackageRedemption) throw new ArgumentException("Choose an item from the current catalog.");
        if(line.EmployeeId.HasValue && !db.Set<Employee>().Any(e=>e.Id==line.EmployeeId && e.IsActive)) throw new ArgumentException("Select an active employee.");
        switch(line.Kind)
        {
            case SaleItemKind.Product:
                var product=db.Set<Product>().Find(line.CatalogItemId!.Value)??throw new ArgumentException("Product not found.");if(!product.IsActive || !product.IsRetail) throw new ArgumentException("This product is unavailable for retail sale.");
                if(product.ExpirationDateUtc?.AddHours(8).Date<DateTime.UtcNow.AddHours(8).Date) throw new ArgumentException("This product is expired.");
                if(line.UnitPrice!=product.SellingPrice && !user.AllowCustomPrices) throw new UnauthorizedAccessException("This account cannot change product prices.");if(line.Charges.Count!=0) throw new ArgumentException("Additional charges apply to services only.");line.Name=product.Name;break;
            case SaleItemKind.Service:
                var service=db.Set<SalonService>().Find(line.CatalogItemId!.Value)??throw new ArgumentException("Service not found.");if(!service.IsActive) throw new ArgumentException("This service is inactive.");
                var standard=service.PricingType switch {PricingType.Fixed=>line.UnitPrice==service.BasePrice,PricingType.Range=>line.UnitPrice>=(service.MinimumPrice??service.BasePrice) && line.UnitPrice<=(service.MaximumPrice??service.BasePrice),PricingType.StartsAt=>line.UnitPrice>=(service.MinimumPrice??service.BasePrice),_=>false};
                var priceRules=db.Set<ServicePriceRule>().Where(r=>r.ServiceId==service.Id).ToList();
                bool Matches(ServicePriceRule rule)=>(!rule.MinimumPrice.HasValue || line.UnitPrice>=rule.MinimumPrice) && (!rule.MaximumPrice.HasValue || line.UnitPrice<=rule.MaximumPrice);
                if(line.PriceRuleId.HasValue)
                {
                    var selected=priceRules.SingleOrDefault(r=>r.Id==line.PriceRuleId)??throw new ArgumentException("The selected price rule does not belong to this service.");
                    if(!Matches(selected)) throw new ArgumentException("The agreed price is outside the selected rule's limits.");
                    if(selected.RequiresAuthorization && !user.AllowCustomPrices) throw new UnauthorizedAccessException("This price rule requires an account authorized for custom prices.");standard=true;
                }
                else if(priceRules.Any(r=>!r.RequiresAuthorization && Matches(r))) standard=true;
                if(!standard && !user.AllowCustomPrices) throw new UnauthorizedAccessException("This account cannot use this custom service price.");
                if(line.Charges.Count>0 && !user.AllowCustomPrices)
                {
                    var presets=db.Set<ServiceChargePreset>().Where(p=>p.IsActive && (p.ServiceId==null || p.ServiceId==service.Id)).ToList();
                    if(line.Charges.Any(c=>!presets.Any(p=>p.Name==c.Name && p.Amount==c.Amount))) throw new UnauthorizedAccessException("Custom additional charges require an authorized account.");
                }
                line.Name=service.Name;break;
            case SaleItemKind.Package:
                if(!customerId.HasValue) throw new ArgumentException("Select a customer before purchasing a package.");var package=db.Set<PackageOffer>().Find(line.CatalogItemId!.Value)??throw new ArgumentException("Package not found.");if(!package.IsActive || !db.Set<SalonService>().Any(s=>s.Id==package.ServiceId && s.IsActive)) throw new ArgumentException("This package or its service is inactive.");
                if(line.UnitPrice!=package.Price && !user.AllowCustomPrices) throw new UnauthorizedAccessException("This account cannot change a package price.");if(line.Charges.Count!=0) throw new ArgumentException("Package purchases do not accept additional charges.");line.Name=package.Name;break;
            case SaleItemKind.PackageRedemption:
                if(!line.CustomerPackageId.HasValue || line.Quantity!=1) throw new ArgumentException("Select one available prepaid session.");var prepaid=db.Set<CustomerPackage>().Find(line.CustomerPackageId.Value)??throw new ArgumentException("Customer package not found.");if(customerId!=prepaid.CustomerId) throw new ArgumentException("The package belongs to a different customer.");line.Name=prepaid.Name+" — prepaid session";line.CatalogItemId=prepaid.ServiceId;break;
            default:throw new ArgumentException("Invalid cart item type.");
        }
    }
    public void AdjustStock(Guid productId,decimal quantityDelta,decimal unitCost,string reason,UserAccount user,bool isAdjustment=false)
    {
        var account=Require(user,true);if(quantityDelta==0 || quantityDelta!=decimal.Round(quantityDelta,6) || unitCost<0 || unitCost!=decimal.Round(unitCost,6) || string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Enter a nonzero quantity, valid cost, and reason.");
        lock(_database.Gate)
        {
            using var db=_database.OpenContext();using var transaction=db.Database.BeginTransaction();var product=db.Set<Product>().Find(productId)??throw new ArgumentException("Product not found.");
            if(quantityDelta>0 && !isAdjustment) {product.PurchaseCost=unitCost;product.CostInformationComplete=true;}
            MoveStock(db,product,quantityDelta,quantityDelta>0 && !isAdjustment?unitCost:product.AverageCost,isAdjustment?InventoryMovementType.Adjustment:(quantityDelta>0?InventoryMovementType.StockIn:InventoryMovementType.StockOut),reason,account.Id);
            AuthService.Audit(db,account.Id,"Inventory adjusted",product.Id,$"Quantity {quantityDelta}; Unit cost PHP {unitCost}; {reason}","Product");db.SaveChanges();transaction.Commit();
        }
    }
    public void SetStockCount(Guid productId,decimal countedQuantity,string reason,UserAccount user)
    {
        var account=Require(user,true);if(countedQuantity<0 || countedQuantity!=decimal.Round(countedQuantity,6) || string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Enter a nonnegative stock count and reason.");
        lock(_database.Gate)
        {
            using var db=_database.OpenContext();using var transaction=db.Database.BeginTransaction();var product=db.Set<Product>().Find(productId)??throw new ArgumentException("Product not found.");
            var delta=countedQuantity-product.StockQuantity;if(delta==0) return;
            MoveStock(db,product,delta,product.AverageCost,InventoryMovementType.Adjustment,reason,account.Id);AuthService.Audit(db,account.Id,"Inventory counted",product.Id,$"Count {countedQuantity}; adjustment {delta}; {reason}","Product");db.SaveChanges();transaction.Commit();
        }
    }
    private static decimal MoveStock(SalonDbContext db,Product product,decimal delta,decimal unitCost,InventoryMovementType type,string reason,Guid userId,Guid? saleItemId=null,Guid? redemptionId=null,Guid? refundId=null,decimal? valueOverride=null)
    {
        if(product.StockQuantity+delta<0) throw new ArgumentException($"Insufficient stock for {product.Name}. Available: {product.StockQuantity} {product.StockUnit}.");
        var movedValue=delta>0?(valueOverride??FinancialEngine.RoundMoney(delta*unitCost)):(-delta==product.StockQuantity?product.InventoryValue:Math.Min(product.InventoryValue,FinancialEngine.RoundMoney(-delta*product.AverageCost)));
        product.InventoryValue+=delta>0?movedValue:-movedValue;
        product.StockQuantity+=delta;
        if(delta>0 && product.StockQuantity>0 && type!=InventoryMovementType.Adjustment) product.AverageCost=decimal.Round(product.InventoryValue/product.StockQuantity,6,MidpointRounding.AwayFromZero);
        db.Add(new InventoryMovement {ProductId=product.Id,Type=type,Quantity=delta,UnitCost=unitCost,ValueChange=delta>0?movedValue:-movedValue,BalanceAfter=product.StockQuantity,Reason=reason,RecordedByUserId=userId,SaleItemId=saleItemId,PackageRedemptionId=redemptionId,RefundId=refundId});
        var average=db.Set<InventoryAverageCost>().Local.SingleOrDefault(a=>a.ProductId==product.Id)??db.Set<InventoryAverageCost>().SingleOrDefault(a=>a.ProductId==product.Id);
        if(average is null) {average=new InventoryAverageCost{ProductId=product.Id};db.Add(average);}average.Quantity=product.StockQuantity;average.UnitCost=product.AverageCost;average.InventoryValue=product.InventoryValue;average.UpdatedAtUtc=DateTime.UtcNow;return movedValue;
    }
    private static (decimal Cost,bool Complete) ConsumeMaterials(SalonDbContext db,SalonService service,decimal quantity,Dictionary<Guid,decimal> extras,Guid userId,Guid? saleItemId,Guid? redemptionId)
    {
        var required=db.Set<ServiceMaterialRequirement>().Where(r=>r.ServiceId==service.Id).ToList().ToDictionary(r=>r.ProductId,r=>r.Quantity*quantity);
        foreach(var extra in extras) { if(extra.Value==0) continue;FinancialEngine.ValidateQuantity(extra.Value);required[extra.Key]=required.GetValueOrDefault(extra.Key)+extra.Value; }
        var total=0m;var complete=service.CostInformationComplete;
        foreach(var material in required)
        {
            var product=db.Set<Product>().Find(material.Key)??throw new ArgumentException("A service material is missing.");if(!product.IsActive) throw new ArgumentException($"Material {product.Name} is inactive.");if(product.ExpirationDateUtc?.AddHours(8).Date<DateTime.UtcNow.AddHours(8).Date) throw new ArgumentException($"Material {product.Name} is expired.");
            complete &= product.CostInformationComplete;
            var cost=MoveStock(db,product,-material.Value,product.AverageCost,InventoryMovementType.ServiceConsumption,service.Name+" material consumption",userId,saleItemId,redemptionId);total+=cost;
            db.Add(new ServiceMaterialConsumption {ServiceId=service.Id,ProductId=product.Id,SaleItemId=saleItemId,PackageRedemptionId=redemptionId,Quantity=material.Value,UnitCost=product.AverageCost,TotalCost=cost,RecordedByUserId=userId});
        }
        return(total,complete);
    }
    private static decimal CreateCommission(SalonDbContext db,Guid? employeeId,Guid serviceId,decimal basis,decimal quantity,Guid userId,Guid? saleItemId,Guid? redemptionId)
    {
        if(!employeeId.HasValue) return 0;
        var employee=db.Set<Employee>().Find(employeeId.Value)??throw new ArgumentException("Employee not found.");if(!employee.IsActive) throw new ArgumentException("Employee is inactive.");
        var custom=db.Set<EmployeeServiceCommission>().SingleOrDefault(c=>c.EmployeeId==employee.Id && c.ServiceId==serviceId);var type=custom?.Type??employee.CommissionType;var rate=custom?.Rate??employee.CommissionRate;
        var amount=FinancialEngine.CalculateCommission(type,rate,basis,quantity);
        if(amount>0) db.Add(new CommissionRecord {EmployeeId=employee.Id,SaleItemId=saleItemId,PackageRedemptionId=redemptionId,Type=type,Rate=rate,BasisAmount=basis,Amount=amount});return amount;
    }
    public PackageRedemption RedeemPackage(Guid customerPackageId,Guid? employeeId,UserAccount user)
    {
        var account=Require(user);
        if(account.Role==RoleKind.SalonStaff && (account.EmployeeId is null || employeeId!=account.EmployeeId)) throw new UnauthorizedAccessException("Staff may redeem their assigned sessions only.");
        lock(_database.Gate) {using var db=_database.OpenContext();using var transaction=db.Database.BeginTransaction();var redemption=RedeemWithinTransaction(db,customerPackageId,employeeId,account,null,[]);db.SaveChanges();transaction.Commit();return redemption;}
    }
    private static PackageRedemption RedeemWithinTransaction(SalonDbContext db,Guid packageId,Guid? employeeId,UserAccount user,Guid? saleItemId,Dictionary<Guid,decimal> extras)
    {
        var package=db.Set<CustomerPackage>().Find(packageId)??throw new ArgumentException("Customer package not found.");if(package.IsCancelled || package.RemainingSessions<=0) throw new ArgumentException("No package sessions remain.");if(package.ExpiresAtUtc<DateTime.UtcNow) throw new ArgumentException("This package has expired.");
        if(user.Role==RoleKind.SalonStaff && !db.Set<Appointment>().Any(a=>a.CustomerId==package.CustomerId && a.EmployeeId==user.EmployeeId && a.ServiceId==package.ServiceId && a.Status!=AppointmentStatus.Cancelled)) throw new UnauthorizedAccessException("This customer is not assigned to you.");
        var service=db.Set<SalonService>().Find(package.ServiceId)??throw new ArgumentException("Package service not found.");var redemption=new PackageRedemption {CustomerPackageId=package.Id,EmployeeId=employeeId,SaleItemId=saleItemId,RecordedByUserId=user.Id};db.Add(redemption);
        var materials=ConsumeMaterials(db,service,1,extras,user.Id,saleItemId,redemption.Id);redemption.MaterialCost=materials.Cost;redemption.CostInformationComplete=materials.Complete;
        var sessionValues=FinancialEngine.AllocateMoney(package.PurchasePrice,Enumerable.Repeat(package.PurchasePrice/package.TotalSessions,package.TotalSessions).ToArray());
        redemption.CommissionCost=CreateCommission(db,employeeId,service.Id,sessionValues[package.UsedSessions],1,user.Id,saleItemId,redemption.Id);package.UsedSessions++;
        AuthService.Audit(db,user.Id,"Package session redeemed",package.Id,$"{package.Name}; remaining {package.RemainingSessions}","CustomerPackage");return redemption;
    }
    private static string BuildReceipt(SalonDbContext db,Sale sale,List<SaleItem> items,OrderCalculation order,PaymentCalculation payment,UserAccount cashier)
    {
        string Setting(string key,string fallback="")=>db.Set<ApplicationSetting>().SingleOrDefault(s=>s.Key==key)?.Value??fallback;
        var text=new StringBuilder();text.AppendLine(Setting("BusinessName","Roselie's Beauty Lounge"));text.AppendLine(Setting("BusinessAddress"));text.AppendLine(Setting("BusinessContact"));
        var registration=Setting("TaxRegistrationDetails");if(registration.Length>0) text.AppendLine(registration);
        text.AppendLine("INTERNAL SALES RECEIPT");text.AppendLine("Tax and BIR configuration requires owner review.");text.AppendLine(sale.TransactionNumber);text.AppendLine(sale.CompletedAtUtc.AddHours(8).ToString("yyyy-MM-dd hh:mm tt"));text.AppendLine("Cashier: "+cashier.Username);
        if(sale.CustomerId.HasValue) text.AppendLine("Customer: "+db.Set<Customer>().Find(sale.CustomerId.Value)?.FullName);
        text.AppendLine(new string('-',32));
        foreach(var item in items)
        {
            text.AppendLine($"{item.Name}");text.AppendLine($" {item.Quantity:0.######} x PHP {item.UnitPrice:0.00} = {item.BaseAmount:0.00}");
            foreach(var charge in order.Lines[items.IndexOf(item)].Charges) text.AppendLine($" + {charge.Name}: {charge.Total:0.00}");
            if(item.DiscountAmount>0) text.AppendLine($" Discount {item.DiscountPercent:0.##}%: -{item.DiscountAmount:0.00}");if(item.OrderDiscountAmount>0) text.AppendLine($" Order discount {sale.DiscountPercent:0.##}%: -{item.OrderDiscountAmount:0.00}");text.AppendLine($" Line total: PHP {item.Total:0.00}");
        }
        text.AppendLine(new string('-',32));text.AppendLine($"Subtotal: PHP {sale.Subtotal:0.00}");text.AppendLine($"Discounts: PHP {sale.DiscountAmount:0.00}");text.AppendLine($"TOTAL: PHP {sale.Total:0.00}");
        foreach(var p in payment.Payments) {text.AppendLine($"{p.Method}: PHP {p.Amount:0.00}");if(p.Reference.Length>0) text.AppendLine(" Reference: "+p.Reference);}
        text.AppendLine($"Received: PHP {sale.AmountReceived:0.00}");text.AppendLine($"Change: PHP {sale.Change:0.00}");text.AppendLine("Thank you for visiting Roselie's!");return text.ToString();
    }
}




