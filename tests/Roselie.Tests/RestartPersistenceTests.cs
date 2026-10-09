using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class RestartPersistenceTests
{
    [Fact]
    public void IndependentDatabaseAndServicesReopenSavedBusinessRecordsAndChangedPassword()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);
        var customer = business.Save(new Customer { FullName = "Isolated restart customer", Phone = "09123456789", Notes = "Saved before independent reopen" }, salon.Admin);
        var product = business.Save(new Product { Name = "Isolated restart product", Sku = "RESTART-STOCK", PurchaseCost = 40.10m, SellingPrice = 125.50m, CostInformationComplete = true }, salon.Admin);
        business.AdjustStock(product.Id, 5, 40.10m, "Isolated restart stock purchase", salon.Admin);
        var expense = business.Save(new Expense { Category = "Utilities", Description = "Isolated restart operating expense", Amount = 34.56m, PaymentMethod = PaymentMethod.Cash }, salon.Admin);
        var service = business.List<SalonService>(salon.Admin).First();
        var appointment = business.Save(new Appointment { CustomerId = customer.Id, ServiceId = service.Id, StartsAtUtc = DateTime.UtcNow.AddDays(1), EndsAtUtc = DateTime.UtcNow.AddDays(1).AddMinutes(30), Status = AppointmentStatus.Confirmed, Notes = "Saved appointment" }, salon.Admin);
        business.SetSetting("BusinessContact", "Saved contact value", salon.Admin);
        var request = new CheckoutRequest
        {
            CustomerId = customer.Id, TransactionDiscountPercent = 10, DiscountReason = "Isolated restart test", Notes = "Committed sale before reopen",
            Lines = [new() { CatalogItemId = product.Id, Name = product.Name, Kind = SaleItemKind.Product, Quantity = 2, UnitPrice = 125.50m }],
            Payments = [new() { Method = PaymentMethod.GCash, Amount = 100, Reference = "RESTART-GCASH" }, new() { Method = PaymentMethod.Cash, Amount = 150 }]
        };
        var sale = business.Checkout(request, salon.Admin);var receipt = business.GetReceipt(sale.Id, salon.Admin);
        var from = DateTime.UtcNow.Date.AddDays(-1);var until = DateTime.UtcNow.Date.AddDays(2);
        var report = business.GetReport(from, until, salon.Admin);
        Assert.Equal(225.90m, sale.Total);Assert.Equal(80.20m, sale.CostOfGoods);Assert.Equal(111.14m, report.NetProfit);
        const string replacement = "restart-owner-password-2026!";
        salon.Auth.ChangeOwnPassword(TestSalon.Password, replacement, salon.Admin);

        // Use a new encryption-key loader, authentication service and business service. No original session is reused.
        var reopenedDatabase = new DatabaseService(salon.DirectoryPath);reopenedDatabase.Initialize();
        Assert.True(reopenedDatabase.CheckIntegrity());
        var reopenedAuth = new AuthService(reopenedDatabase);Assert.False(reopenedAuth.FirstRunRequired);Assert.Null(reopenedAuth.CurrentUser);
        var reopenedUser = reopenedAuth.Login("administrator", replacement);Assert.NotSame(salon.Admin, reopenedUser);
        var reopenedBusiness = new BusinessService(reopenedDatabase, reopenedAuth);
        var savedCustomer = Assert.Single(reopenedBusiness.List<Customer>(reopenedUser));
        Assert.Equal(customer.Id, savedCustomer.Id);Assert.Equal(customer.Phone, savedCustomer.Phone);Assert.Equal(customer.Notes, savedCustomer.Notes);
        var savedProduct = Assert.Single(reopenedBusiness.List<Product>(reopenedUser));
        Assert.Equal(product.Id, savedProduct.Id);Assert.Equal(3m, savedProduct.StockQuantity);Assert.Equal(120.30m, savedProduct.InventoryValue);Assert.Equal(40.10m, savedProduct.AverageCost);
        var savedExpense = Assert.Single(reopenedBusiness.List<Expense>(reopenedUser));
        Assert.Equal(expense.Id, savedExpense.Id);Assert.Equal(34.56m, savedExpense.Amount);Assert.Equal(expense.Description, savedExpense.Description);
        var savedAppointment = Assert.Single(reopenedBusiness.List<Appointment>(reopenedUser));
        Assert.Equal(appointment.Id, savedAppointment.Id);Assert.Equal(AppointmentStatus.Confirmed, savedAppointment.Status);Assert.Equal(appointment.StartsAtUtc, savedAppointment.StartsAtUtc);
        var savedSale = Assert.Single(reopenedBusiness.List<Sale>(reopenedUser));
        Assert.Equal(sale.Id, savedSale.Id);Assert.Equal(sale.TransactionNumber, savedSale.TransactionNumber);Assert.Equal(225.90m, savedSale.Total);Assert.Equal(25.10m, savedSale.DiscountAmount);Assert.Equal(24.10m, savedSale.Change);Assert.Equal(sale.Notes, savedSale.Notes);
        var savedItem = Assert.Single(reopenedBusiness.List<SaleItem>(reopenedUser));
        Assert.Equal(2m, savedItem.Quantity);Assert.Equal(125.50m, savedItem.UnitPrice);Assert.Equal(80.20m, savedItem.CostOfGoods);Assert.Equal(25.10m, savedItem.OrderDiscountAmount);
        var payments = reopenedBusiness.List<Payment>(reopenedUser);Assert.Equal(2, payments.Count);
        Assert.Equal(100m, Assert.Single(payments.Where(p => p.Method == PaymentMethod.GCash)).AppliedAmount);
        Assert.Equal("RESTART-GCASH", Assert.Single(payments.Where(p => p.Method == PaymentMethod.GCash)).Reference);
        Assert.Equal(125.90m, Assert.Single(payments.Where(p => p.Method == PaymentMethod.Cash)).AppliedAmount);
        Assert.Equal(receipt.Content, reopenedBusiness.GetReceipt(sale.Id, reopenedUser).Content);
        Assert.Equal("Saved contact value", reopenedBusiness.GetSetting("BusinessContact"));
        Assert.Equal(40, reopenedBusiness.List<SalonService>(reopenedUser).Count);Assert.Equal(21, reopenedBusiness.List<PackageOffer>(reopenedUser).Count);
        Assert.Equal(2, reopenedBusiness.List<InventoryMovement>(reopenedUser).Count);
        Assert.Contains(reopenedBusiness.List<AuditLog>(reopenedUser), a => a.Action == "Administrator password changed");
        var savedReport = reopenedBusiness.GetReport(from, until, reopenedUser);
        Assert.Equal(report.NetSales, savedReport.NetSales);Assert.Equal(report.CostOfGoods, savedReport.CostOfGoods);Assert.Equal(report.OperatingExpenses, savedReport.OperatingExpenses);Assert.Equal(report.NetProfit, savedReport.NetProfit);

        // The persisted checkout token still prevents a second charge and second stock deduction after reopening.
        Assert.Equal(sale.Id, reopenedBusiness.Checkout(request, reopenedUser).Id);
        Assert.Single(reopenedBusiness.List<Sale>(reopenedUser));Assert.Equal(3m, Assert.Single(reopenedBusiness.List<Product>(reopenedUser)).StockQuantity);
        reopenedAuth.Logout();
        var secondDatabase = new DatabaseService(salon.DirectoryPath);secondDatabase.Initialize();
        Assert.True(secondDatabase.CheckIntegrity());var secondAuth = new AuthService(secondDatabase);
        var secondBusiness = new BusinessService(secondDatabase, secondAuth);var secondUser = secondAuth.Login("administrator", replacement);
        Assert.Equal(sale.Id, Assert.Single(secondBusiness.List<Sale>(secondUser)).Id);
        Assert.Equal(receipt.Content, secondBusiness.GetReceipt(sale.Id, secondUser).Content);
        Assert.Equal(111.14m, secondBusiness.GetReport(from, until, secondUser).NetProfit);
    }
}
