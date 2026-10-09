using Roselie.Core.Finance;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class TransactionTests
{
    private static Product AddStock(TestSalon salon, BusinessService business, string name, decimal quantity,
        decimal cost, decimal price = 100, bool retail = true)
    {
        var product = business.Save(new Product
        {
            Name = name, Sku = Guid.NewGuid().ToString("N"), PurchaseCost = cost,
            SellingPrice = price, IsRetail = retail, CostInformationComplete = true
        }, salon.Admin);
        business.AdjustStock(product.Id, quantity, cost, "Isolated test stock", salon.Admin);
        return salon.Read<Product>(product.Id);
    }

    private static CartLineInput ProductLine(Product product, decimal quantity = 1, decimal discount = 0) => new()
    {
        CatalogItemId = product.Id, Name = product.Name, Kind = SaleItemKind.Product,
        UnitPrice = product.SellingPrice, Quantity = quantity, DiscountPercent = discount
    };

    private static CheckoutRequest Request(params CartLineInput[] lines)
    {
        var total = FinancialEngine.CalculateOrder(lines).Total;
        return new CheckoutRequest
        {
            Lines = lines.ToList(), Payments = total > 0
                ? [new PaymentInput { Method = PaymentMethod.Cash, Amount = total }]
                : []
        };
    }

    [Fact]
    public void CheckoutTokenPreventsDuplicateSalesAndStockDeductions()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var product = AddStock(salon, business, "Retail test", 5, 50);
        var request = Request(ProductLine(product, 2));
        var first = business.Checkout(request, salon.Admin);
        var second = business.Checkout(request, salon.Admin);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, salon.Count<Sale>());
        Assert.Equal(1, salon.Count<SaleItem>());
        Assert.Equal(1, salon.Count<Payment>());
        Assert.Equal(1, salon.Count<ReceiptRecord>());
        Assert.Equal(3m, salon.Read<Product>(product.Id).StockQuantity);
        Assert.Equal(100m, salon.Read<Sale>(first.Id).CostOfGoods);
    }

    [Fact]
    public void InsufficientStockRollsBackEveryLineAndSaleSnapshot()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var first = AddStock(salon, business, "Available", 3, 50);
        var second = AddStock(salon, business, "Insufficient", 1, 50);
        var movementCount = salon.Count<InventoryMovement>();
        Assert.ThrowsAny<Exception>(() => business.Checkout(Request(ProductLine(first), ProductLine(second, 2)), salon.Admin));
        Assert.Equal(3m, salon.Read<Product>(first.Id).StockQuantity);
        Assert.Equal(1m, salon.Read<Product>(second.Id).StockQuantity);
        Assert.Equal(0, salon.Count<Sale>());
        Assert.Equal(0, salon.Count<SaleItem>());
        Assert.Equal(0, salon.Count<Payment>());
        Assert.Equal(0, salon.Count<ReceiptRecord>());
        Assert.Equal(movementCount, salon.Count<InventoryMovement>());
    }

    [Fact]
    public void InsufficientServiceMaterialRollsBackEarlierRetailDeduction()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var retail = AddStock(salon, business, "Available retail", 3, 50);
        var material = AddStock(salon, business, "Insufficient dye", 1, 10, retail: false);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Hair Color");
        business.Save(new ServiceMaterialRequirement { ServiceId = service.Id, ProductId = material.Id, Quantity = 2 }, salon.Admin);
        var serviceLine = new CartLineInput { CatalogItemId = service.Id, Name = service.Name, UnitPrice = 500, Kind = SaleItemKind.Service };
        Assert.ThrowsAny<Exception>(() => business.Checkout(Request(ProductLine(retail), serviceLine), salon.Admin));
        Assert.Equal(3m, salon.Read<Product>(retail.Id).StockQuantity);
        Assert.Equal(1m, salon.Read<Product>(material.Id).StockQuantity);
        Assert.Equal(0, salon.Count<Sale>());
        Assert.Equal(0, salon.Count<ServiceMaterialConsumption>());
    }

    [Fact]
    public void WeightedAverageCostsPersistAndReconcileWithSaleAndReport()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var product = AddStock(salon, business, "Weighted average", 2, 10);
        business.AdjustStock(product.Id, 1, 20, "Second cost batch", salon.Admin);
        var updated = salon.Read<Product>(product.Id);
        Assert.Equal(13.333333m, updated.AverageCost);
        Assert.Equal(3m, updated.StockQuantity);
        var sale = business.Checkout(Request(ProductLine(updated, 3)), salon.Admin);
        Assert.Equal(40m, sale.CostOfGoods);
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(300m, report.NetSales);
        Assert.Equal(40m, report.CostOfGoods);
        Assert.Equal(260m, report.NetProfit);
        Assert.Equal(0m, salon.Read<Product>(product.Id).StockQuantity);
    }

    [Fact]
    public void SaleCostsDiscountsCommissionsExpenseAndRefundReconcile()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var retail = AddStock(salon, business, "Retail", 10, 50);
        var material = AddStock(salon, business, "Dye", 10, 80, retail: false);
        var employee = business.Save(new Employee { FullName = "Test employee", CommissionType = CommissionType.Percentage, CommissionRate = 10 }, salon.Admin);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Hair Color");
        service.CostInformationComplete = true;
        business.Save(service, salon.Admin);
        business.Save(new ServiceMaterialRequirement { ServiceId = service.Id, ProductId = material.Id, Quantity = 2 }, salon.Admin);
        var serviceLine = new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, Kind = SaleItemKind.Service, UnitPrice = 600,
            EmployeeId = employee.Id, DiscountPercent = 10,
            Charges = [new AdditionalChargeInput { Name = "Long Hair", Amount = 300 }]
        };
        var sale = business.Checkout(Request(ProductLine(retail, 2, 10), serviceLine), salon.Admin);
        business.Save(new Expense { Category = "Rent", Description = "Isolated test expense", Amount = 100 }, salon.Admin);
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(990m, report.NetSales);
        Assert.Equal(100m, report.CostOfGoods);
        Assert.Equal(160m, report.MaterialCost);
        Assert.Equal(81m, report.CommissionCost);
        Assert.Equal(100m, report.OperatingExpenses);
        Assert.Equal(549m, report.NetProfit);
        Assert.Equal(sale.Total, report.NetSales);
        Assert.Equal(sale.Total, report.CashCollections);
        Assert.Equal(0, report.IncompleteCostTransactions);
        var refund = business.RefundSale(sale.Id, "Full test refund", PaymentMethod.Cash, salon.Admin);
        Assert.Equal(990m, refund.Amount);
        Assert.Equal(10m, salon.Read<Product>(retail.Id).StockQuantity);
        Assert.Equal(8m, salon.Read<Product>(material.Id).StockQuantity);
        var after = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(0m, after.NetSales);
        Assert.Equal(0m, after.CostOfGoods);
        Assert.Equal(160m, after.MaterialCost);
        Assert.Equal(0m, after.CommissionCost);
        Assert.Equal(-260m, after.NetProfit);
        Assert.ThrowsAny<Exception>(() => business.RefundSale(sale.Id, "Duplicate refund", PaymentMethod.Cash, salon.Admin));
    }

    [Fact]
    public void CommissionPayoutDoesNotDoubleCountAccruedCommissionExpense()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var employee = business.Save(new Employee { FullName = "Commission test", CommissionType = CommissionType.Fixed, CommissionRate = 50 }, salon.Admin);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Regular Manicure");
        business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = service.BasePrice,
            Kind = SaleItemKind.Service, EmployeeId = employee.Id
        }), salon.Admin);
        var commission = Assert.Single(business.List<CommissionRecord>(salon.Admin));
        business.Save(new Expense { Category = "Commissions", Description = "Payout", Amount = 50, CommissionRecordId = commission.Id }, salon.Admin);
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(50m, report.CommissionCost);
        Assert.Equal(0m, report.OperatingExpenses);
        Assert.Equal(100m, report.NetProfit);
    }

    [Fact]
    public void SixPackageSessionsDeductMaterialsWithoutCreatingNewSalesRevenue()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var customer = business.Save(new Customer { FullName = "Package test customer" }, salon.Admin);
        var offer = business.List<PackageOffer>(salon.Admin).Single(x => x.Price == 2995 && x.Name.StartsWith("Brightening"));
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Id == offer.ServiceId);
        var material = AddStock(salon, business, "Package material", 10, 10, retail: false);
        service.CostInformationComplete = true;
        business.Save(service, salon.Admin);
        business.Save(new ServiceMaterialRequirement { ServiceId = service.Id, ProductId = material.Id, Quantity = 1 }, salon.Admin);
        var request = Request(new CartLineInput { CatalogItemId = offer.Id, Name = offer.Name, UnitPrice = offer.Price, Kind = SaleItemKind.Package });
        request.CustomerId = customer.Id;
        business.Checkout(request, salon.Admin);
        var entitlement = Assert.Single(business.List<CustomerPackage>(salon.Admin));
        for (var i = 0; i < 6; i++) business.RedeemPackage(entitlement.Id, null, salon.Admin);
        Assert.Equal(0, salon.Read<CustomerPackage>(entitlement.Id).RemainingSessions);
        Assert.Equal(4m, salon.Read<Product>(material.Id).StockQuantity);
        Assert.Equal(6, salon.Count<PackageRedemption>());
        Assert.Equal(1, salon.Count<Sale>());
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(2995m, report.NetSales);
        Assert.Equal(60m, report.MaterialCost);
        Assert.Equal(2935m, report.NetProfit);
        Assert.ThrowsAny<Exception>(() => business.RedeemPackage(entitlement.Id, null, salon.Admin));
        Assert.ThrowsAny<Exception>(() => business.RefundSale(business.List<Sale>(salon.Admin).Single().Id, "Already used package", PaymentMethod.Cash, salon.Admin));
    }

    [Fact]
    public void AppointmentsRejectOverlapsButAllowAdjacentAndCancelledSlots()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var customer = business.Save(new Customer { FullName = "Appointment test" }, salon.Admin);
        var employee = business.Save(new Employee { FullName = "Assigned employee" }, salon.Admin);
        var service = business.List<SalonService>(salon.Admin).First();
        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(2);
        Appointment Slot(int startMinute, int endMinute, AppointmentStatus status = AppointmentStatus.Confirmed) => new()
        {
            CustomerId = customer.Id, EmployeeId = employee.Id, ServiceId = service.Id,
            StartsAtUtc = start.AddMinutes(startMinute), EndsAtUtc = start.AddMinutes(endMinute), Status = status
        };
        business.Save(Slot(0, 30), salon.Admin);
        Assert.Throws<ArgumentException>(() => business.Save(Slot(15, 45), salon.Admin));
        business.Save(Slot(30, 60), salon.Admin);
        business.Save(Slot(0, 30, AppointmentStatus.Cancelled), salon.Admin);
        Assert.Equal(3, salon.Count<Appointment>());
    }

    [Fact]
    public void CashierLimitsPreventUnauthorizedPricesDiscountsAndManagement()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var product = AddStock(salon, business, "Permission test", 5, 50);
        salon.Auth.CreateUser("cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var cashier = salon.Auth.Login("cashier", TestSalon.Password);
        Assert.Throws<UnauthorizedAccessException>(() => business.AdjustStock(product.Id, 1, 50, "Unauthorized", cashier));
        Assert.Throws<UnauthorizedAccessException>(() => business.Save(new Expense { Amount = 1, Description = "Unauthorized" }, cashier));
        Assert.Throws<UnauthorizedAccessException>(() => business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), cashier));
        Assert.ThrowsAny<Exception>(() => business.Checkout(Request(ProductLine(product, discount: 10.01m)), cashier));
        var alteredPrice = ProductLine(product);
        alteredPrice.UnitPrice = 90;
        Assert.ThrowsAny<Exception>(() => business.Checkout(Request(alteredPrice), cashier));
        var sale = business.Checkout(Request(ProductLine(product, discount: 10)), cashier);
        Assert.Equal(90m, sale.Total);
        Assert.Equal(4m, salon.Read<Product>(product.Id).StockQuantity);
        Assert.Equal(0m, business.List<Product>(cashier).Single().AverageCost);
        Assert.Equal(0m, business.List<Sale>(cashier).Single().CostOfGoods);
    }

    [Fact]
    public void HistoricalReceiptAndSaleRemainStableAfterCatalogPriceChanges()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Regular Manicure");
        var sale = business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = 150, Kind = SaleItemKind.Service
        }), salon.Admin);
        var before = business.GetReceipt(sale.Id, salon.Admin).Content;
        service.BasePrice = 999;
        service.Name = "Updated catalog name";
        business.Save(service, salon.Admin);
        Assert.Equal(150m, salon.Read<Sale>(sale.Id).Total);
        var item = Assert.Single(business.List<SaleItem>(salon.Admin));
        Assert.Equal(150m, item.UnitPrice);
        Assert.Equal("Regular Manicure", item.Name);
        Assert.Equal(before, business.GetReceipt(sale.Id, salon.Admin).Content);
    }

    [Fact]
    public void ActualExtraMaterialUsageIsDeductedAndCostedWithConfiguredRequirements()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var material = AddStock(salon, business, "Actual material use", 10, 10, retail: false);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Hair Color");
        business.Save(new ServiceMaterialRequirement { ServiceId = service.Id, ProductId = material.Id, Quantity = 2 }, salon.Admin);
        var sale = business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = 500, Quantity = 2,
            Kind = SaleItemKind.Service, AdditionalMaterialQuantities = new() { [material.Id] = 1 }
        }), salon.Admin);
        Assert.Equal(5m, salon.Read<Product>(material.Id).StockQuantity);
        Assert.Equal(50m, sale.MaterialCost);
        Assert.Equal(5m, Assert.Single(business.List<ServiceMaterialConsumption>(salon.Admin)).Quantity);
    }

    [Fact]
    public void MultiplePackageEntitlementsPreserveEveryDiscountedCent()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var customer = business.Save(new Customer { FullName = "Package cent test" }, salon.Admin);
        var offer = business.List<PackageOffer>(salon.Admin).First();
        var request = Request(new CartLineInput
        {
            CatalogItemId = offer.Id, Name = offer.Name, Kind = SaleItemKind.Package,
            UnitPrice = 0.34m, Quantity = 3, DiscountPercent = 1.96m
        });
        request.CustomerId = customer.Id;
        var sale = business.Checkout(request, salon.Admin);
        var entitlements = business.List<CustomerPackage>(salon.Admin);
        Assert.Equal(3, entitlements.Count);
        Assert.Equal(1m, sale.Total);
        Assert.Equal(sale.Total, entitlements.Sum(x => x.PurchasePrice));
    }

    [Fact]
    public async Task SimultaneousRetriesCommitTheSameCheckoutExactlyOnce()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var product = AddStock(salon, business, "Concurrent token", 5, 50);
        var request = Request(ProductLine(product));
        var results = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() => business.Checkout(request, salon.Admin))));
        Assert.Single(results.Select(x => x.Id).Distinct());
        Assert.Equal(1, salon.Count<Sale>());
        Assert.Equal(4m, salon.Read<Product>(product.Id).StockQuantity);
    }

    [Fact]
    public void RefundWithoutRestockingKeepsTheActualCostOfGoodsExpense()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var product = AddStock(salon, business, "Opened refund product", 5, 50);
        var sale = business.Checkout(Request(ProductLine(product, 2)), salon.Admin);
        var refund = business.RefundSale(sale.Id, "Opened products cannot be resold", PaymentMethod.Cash,
            salon.Admin, returnProductsToStock: false);
        Assert.Equal(0m, refund.CostOfGoodsReversed);
        Assert.Equal(3m, salon.Read<Product>(product.Id).StockQuantity);
        Assert.All(business.List<RefundItem>(salon.Admin), x => Assert.False(x.ReturnToStock));
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(0m, report.NetSales);
        Assert.Equal(100m, report.CostOfGoods);
        Assert.Equal(-100m, report.NetProfit);
    }

    [Fact]
    public void RefundKeepsAlreadyPaidCommissionAsAnExpense()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var employee = business.Save(new Employee { FullName = "Paid commission test", CommissionType = CommissionType.Fixed, CommissionRate = 50 }, salon.Admin);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Regular Manicure");
        var sale = business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = service.BasePrice,
            Kind = SaleItemKind.Service, EmployeeId = employee.Id
        }), salon.Admin);
        var commission = Assert.Single(business.List<CommissionRecord>(salon.Admin));
        business.Save(new Expense { Category = "Commissions", Description = "Already paid", Amount = 50, CommissionRecordId = commission.Id }, salon.Admin);
        var refund = business.RefundSale(sale.Id, "Service refund after payout", PaymentMethod.Cash, salon.Admin);
        Assert.Equal(0m, refund.CommissionCostReversed);
        Assert.False(salon.Read<CommissionRecord>(commission.Id).IsReversed);
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(0m, report.NetSales);
        Assert.Equal(50m, report.CommissionCost);
        Assert.Equal(0m, report.OperatingExpenses);
        Assert.Equal(-50m, report.NetProfit);
    }

    [Fact]
    public void StockLossIsAnExpenseWhilePurchasesAndRetailDeductionsAreNotDoubleCounted()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var product = AddStock(salon, business, "Stock loss test", 5, 50);
        var from = DateTime.UtcNow.Date;
        var to = from.AddDays(1);
        Assert.Equal(0m, business.GetReport(from, to, salon.Admin).OperatingExpenses);
        business.AdjustStock(product.Id, -2, 999, "Damaged stock", salon.Admin);
        var loss = business.GetReport(from, to, salon.Admin);
        Assert.Equal(100m, loss.OperatingExpenses);
        Assert.Equal(-100m, loss.NetProfit);
        business.Checkout(Request(ProductLine(product)), salon.Admin);
        var after = business.GetReport(from, to, salon.Admin);
        Assert.Equal(100m, after.NetSales);
        Assert.Equal(50m, after.CostOfGoods);
        Assert.Equal(100m, after.OperatingExpenses);
        Assert.Equal(-50m, after.NetProfit);
        Assert.Equal(2m, salon.Read<Product>(product.Id).StockQuantity);
    }

    [Fact]
    public void ExpiredServiceMaterialsCannotBeConsumedAndCheckoutIsAtomic()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var material = AddStock(salon, business, "Expired material", 10, 10, retail: false);
        material.ExpirationDateUtc = DateTime.UtcNow.Date.AddDays(-1);
        business.Save(material, salon.Admin);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Hair Color");
        business.Save(new ServiceMaterialRequirement { ServiceId = service.Id, ProductId = material.Id, Quantity = 1 }, salon.Admin);
        Assert.Throws<ArgumentException>(() => business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = 500, Kind = SaleItemKind.Service
        }), salon.Admin));
        Assert.Equal(10m, salon.Read<Product>(material.Id).StockQuantity);
        Assert.Equal(0, salon.Count<Sale>());
        Assert.Equal(0, salon.Count<ServiceMaterialConsumption>());
    }

    [Fact]
    public void ServicePriceRulesApplyTheirBoundsAndAuthorizationRequirements()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Hair Color");
        var unrestricted = business.Save(new ServicePriceRule
        {
            ServiceId = service.Id, Name = "Premium configuration", MinimumPrice = 1200, MaximumPrice = 2000
        }, salon.Admin);
        var restricted = business.Save(new ServicePriceRule
        {
            ServiceId = service.Id, Name = "Owner approval", MinimumPrice = 2500, MaximumPrice = 3000, RequiresAuthorization = true
        }, salon.Admin);
        salon.Auth.CreateUser("cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var cashier = salon.Auth.Login("cashier", TestSalon.Password);
        CartLineInput Priced(decimal price, Guid? ruleId) => new()
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = price, Kind = SaleItemKind.Service,
            PriceRuleId = ruleId
        };
        Assert.Equal(1500m, business.Checkout(Request(Priced(1500, unrestricted.Id)), cashier).Total);
        Assert.ThrowsAny<Exception>(() => business.Checkout(Request(Priced(2100, unrestricted.Id)), cashier));
        Assert.Throws<UnauthorizedAccessException>(() => business.Checkout(Request(Priced(2750, restricted.Id)), cashier));
        var admin = salon.Auth.Login("administrator", TestSalon.Password);
        Assert.Equal(2750m, business.Checkout(Request(Priced(2750, restricted.Id)), admin).Total);
    }

    [Fact]
    public void StartsAtPricingAllowsHigherAmountsWithoutAnArtificialUpperLimit()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Tattoo Removal");
        salon.Auth.CreateUser("cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var cashier = salon.Auth.Login("cashier", TestSalon.Password);
        var sale = business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = 10000, Kind = SaleItemKind.Service
        }), cashier);
        Assert.Equal(10000m, sale.Total);
        Assert.ThrowsAny<Exception>(() => business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = 498, Kind = SaleItemKind.Service
        }), cashier));
    }

    [Fact]
    public void SeparateRetailDepletionsAndRefundsPreserveEveryInventoryCostCent()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var product = AddStock(salon, business, "Exact inventory ledger", 2, 10);
        business.AdjustStock(product.Id, 1, 20, "Second purchase cost", salon.Admin);
        Assert.Equal(40m, salon.Read<Product>(product.Id).InventoryValue);
        var sales = Enumerable.Range(0, 3).Select(_ => business.Checkout(Request(ProductLine(product)), salon.Admin)).ToArray();
        Assert.Equal(new[] { 13.33m, 13.33m, 13.34m }, sales.Select(x => x.CostOfGoods).ToArray());
        Assert.Equal(40m, sales.Sum(x => x.CostOfGoods));
        Assert.Equal(0m, salon.Read<Product>(product.Id).InventoryValue);
        Assert.Equal(0m, salon.Read<Product>(product.Id).StockQuantity);
        Assert.Equal(0m, business.List<InventoryMovement>(salon.Admin).Sum(x => x.ValueChange));
        business.RefundSale(sales[2].Id, "Restock last unit", PaymentMethod.Cash, salon.Admin);
        var returned = salon.Read<Product>(product.Id);
        Assert.Equal(1m, returned.StockQuantity);
        Assert.Equal(13.34m, returned.InventoryValue);
        var resold = business.Checkout(Request(ProductLine(returned)), salon.Admin);
        Assert.Equal(13.34m, resold.CostOfGoods);
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(40m, report.CostOfGoods);
        Assert.Equal(260m, report.NetProfit);
    }

    [Fact]
    public void ServiceMaterialDepletionAlsoConservesTheFinalCostCent()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var material = AddStock(salon, business, "Exact material ledger", 2, 10, retail: false);
        business.AdjustStock(material.Id, 1, 20, "Second material batch", salon.Admin);
        var service = business.List<SalonService>(salon.Admin).Single(x => x.Name == "Regular Manicure");
        business.Save(new ServiceMaterialRequirement { ServiceId = service.Id, ProductId = material.Id, Quantity = 1 }, salon.Admin);
        var costs = Enumerable.Range(0, 3).Select(_ => business.Checkout(Request(new CartLineInput
        {
            CatalogItemId = service.Id, Name = service.Name, UnitPrice = 150, Kind = SaleItemKind.Service
        }), salon.Admin).MaterialCost).ToArray();
        Assert.Equal(new[] { 13.33m, 13.33m, 13.34m }, costs);
        Assert.Equal(0m, salon.Read<Product>(material.Id).InventoryValue);
        var report = business.GetReport(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), salon.Admin);
        Assert.Equal(40m, report.MaterialCost);
        Assert.Equal(410m, report.NetProfit);
    }
}
