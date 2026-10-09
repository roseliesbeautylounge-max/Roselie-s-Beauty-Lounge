using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class SuperPinDeletionTests
{
    private const string SuperPin = "739251";
    private static SalonService Service(TestSalon salon, BusinessService business, bool package = false) => business.Save(new SalonService
    {
        Name = "Isolated protected service", CategoryId = business.List<ServiceCategory>(salon.Admin).First().Id,
        BasePrice = 100, CostInformationComplete = true, OwnerConfirmed = true, PackageAvailable = package, PackagePrice = package ? 600 : null
    }, salon.Admin);

    [Fact]
    public void SeparateSuperPinDeletesServiceWhilePreservingSaleReceiptAndFinancialSnapshots()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);
        salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);
        var service = Service(salon, business, true);
        var sale = business.Checkout(new CheckoutRequest { Lines = [new() { Kind = SaleItemKind.Service, CatalogItemId = service.Id, UnitPrice = 100 }], Payments = [new() { Method = PaymentMethod.Cash, Amount = 100 }] }, salon.Admin);
        var receipt = business.GetReceipt(sale.Id, salon.Admin).Content;
        Assert.Throws<UnauthorizedAccessException>(() => business.DeleteService(service.Id, "482915", salon.Admin, "Owner review"));
        Assert.False(salon.Read<SalonService>(service.Id).IsDeleted);
        business.DeleteService(service.Id, SuperPin, salon.Admin, "Owner retired this treatment");
        var saved = salon.Read<SalonService>(service.Id);Assert.True(saved.IsDeleted);Assert.False(saved.IsActive);
        Assert.DoesNotContain(business.List<SalonService>(salon.Admin), s => s.Id == service.Id);
        using(var db = salon.Database.OpenContext())Assert.All(db.Set<PackageOffer>().Where(p => p.ServiceId == service.Id).ToList(), p => {Assert.True(p.IsDeleted);Assert.False(p.IsActive);});
        Assert.Equal(sale.Total, salon.Read<Sale>(sale.Id).Total);Assert.Equal(receipt, business.GetReceipt(sale.Id, salon.Admin).Content);
        Assert.Equal(service.Name, Assert.Single(business.List<SaleItem>(salon.Admin)).Name);
        Assert.Equal(100m, business.GetReport(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), salon.Admin).NetSales);
        saved.IsDeleted = false;saved.IsActive = true;Assert.Throws<ArgumentException>(() => business.Save(saved, salon.Admin));
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();Assert.True(reopened.CheckIntegrity());
        Assert.True(salon.Read<SalonService>(service.Id).IsDeleted);
    }

    [Fact]
    public void DeletedOfferAndServiceStillHonorPreviouslyPurchasedCustomerSessions()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);
        var service = Service(salon, business);var customer = business.Save(new Customer { FullName = "Isolated prepaid customer" }, salon.Admin);
        var offer = business.Save(new PackageOffer { Name = "Isolated paid sessions", ServiceId = service.Id, Price = 600, TotalSessions = 6 }, salon.Admin);
        var sale = business.Checkout(new CheckoutRequest { CustomerId = customer.Id, Lines = [new() { Kind = SaleItemKind.Package, CatalogItemId = offer.Id, UnitPrice = 600 }], Payments = [new() { Method = PaymentMethod.Cash, Amount = 600 }] }, salon.Admin);
        var entitlement = Assert.Single(business.List<CustomerPackage>(salon.Admin));var receipt = business.GetReceipt(sale.Id, salon.Admin).Content;
        business.DeletePackageOffer(offer.Id, SuperPin, salon.Admin, "Retire offer without cancelling paid visits");
        Assert.True(salon.Read<PackageOffer>(offer.Id).IsDeleted);Assert.DoesNotContain(business.List<PackageOffer>(salon.Admin), p => p.Id == offer.Id);
        business.RedeemPackage(entitlement.Id, null, salon.Admin);
        business.DeleteService(service.Id, SuperPin, salon.Admin, "Retire catalog service; honor existing sessions");
        business.RedeemPackage(entitlement.Id, null, salon.Admin);
        var saved = salon.Read<CustomerPackage>(entitlement.Id);Assert.False(saved.IsCancelled);Assert.Equal(2, saved.UsedSessions);Assert.Equal(4, saved.RemainingSessions);
        Assert.Equal(receipt, business.GetReceipt(sale.Id, salon.Admin).Content);Assert.Single(business.List<Sale>(salon.Admin));
        Assert.Equal(600m, business.GetReport(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), salon.Admin).NetSales);
    }

    [Fact]
    public void ExpenseVoidRetainsOriginalLedgerEntryAndReconcilesActiveReportsOnce()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);
        var expense = business.Save(new Expense { Description = "Isolated incorrect expense", Category = "Utilities", Amount = 50.25m }, salon.Admin);
        business.Save(new Expense { Description = "Isolated valid expense", Category = "Utilities", Amount = 12.75m }, salon.Admin);
        var from = DateTime.UtcNow.AddDays(-1);var until = DateTime.UtcNow.AddDays(1);Assert.Equal(63m, business.GetReport(from, until, salon.Admin).OperatingExpenses);
        expense.IsVoided = true;Assert.Throws<ArgumentException>(() => business.Save(expense, salon.Admin));
        business.VoidExpense(expense.Id, SuperPin, salon.Admin, "Duplicate bill entered by mistake");
        var saved = salon.Read<Expense>(expense.Id);Assert.True(saved.IsVoided);Assert.Equal(50.25m, saved.Amount);Assert.Equal("Duplicate bill entered by mistake", saved.VoidReason);
        Assert.Equal(salon.Admin.Id, saved.VoidedByUserId);Assert.NotNull(saved.VoidedAtUtc);Assert.Equal(2, salon.Count<Expense>());
        Assert.Single(business.List<Expense>(salon.Admin));Assert.Equal(12.75m, business.GetReport(from, until, salon.Admin).OperatingExpenses);
        Assert.Throws<InvalidOperationException>(() => business.VoidExpense(expense.Id, SuperPin, salon.Admin, "Repeat deletion"));
        Assert.Equal(-12.75m, business.GetReport(from, until, salon.Admin).NetProfit);
        using var db = salon.Database.OpenContext();Assert.Single(db.Set<AuditLog>().Where(a => a.Action == "Expense voided" && a.EntityId == expense.Id).ToList());
    }

    [Fact]
    public void VoidedCommissionPayoutKeepsEarnedCostAndAllowsOneReplacementPayment()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);
        var service = Service(salon, business);var employee = business.Save(new Employee { FullName = "Isolated commissioned employee", CommissionType = CommissionType.Fixed, CommissionRate = 10 }, salon.Admin);
        business.Checkout(new CheckoutRequest { Lines = [new() { Kind = SaleItemKind.Service, CatalogItemId = service.Id, UnitPrice = 100, EmployeeId = employee.Id }], Payments = [new() { Method = PaymentMethod.Cash, Amount = 100 }] }, salon.Admin);
        var commission = Assert.Single(business.List<CommissionRecord>(salon.Admin));
        var payout = business.Save(new Expense { Description = "Isolated commission payout", Category = "Commissions", Amount = commission.Amount, CommissionRecordId = commission.Id }, salon.Admin);
        Assert.NotNull(salon.Read<CommissionRecord>(commission.Id).PaidAtUtc);
        business.VoidExpense(payout.Id, SuperPin, salon.Admin, "Incorrect payout record");Assert.Null(salon.Read<CommissionRecord>(commission.Id).PaidAtUtc);
        var replacement = business.Save(new Expense { Description = "Correct replacement payout", Category = "Commissions", Amount = commission.Amount, CommissionRecordId = commission.Id }, salon.Admin);
        Assert.NotEqual(payout.Id, replacement.Id);Assert.NotNull(salon.Read<CommissionRecord>(commission.Id).PaidAtUtc);Assert.Equal(2, salon.Count<Expense>());
        Assert.Throws<ArgumentException>(() => business.Save(new Expense { Description = "Second duplicate payout", Amount = commission.Amount, CommissionRecordId = commission.Id }, salon.Admin));
        var report = business.GetReport(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), salon.Admin);Assert.Equal(10m, report.CommissionCost);Assert.Equal(0m, report.OperatingExpenses);Assert.Equal(90m, report.NetProfit);
        Assert.True(salon.Database.CheckIntegrity());
    }

    [Fact]
    public void SuperPinFailuresPersistLockoutWithoutLockingNormalPasswordSignIn()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);var service = Service(salon, business);
        for(var i = 0; i < 5; i++)Assert.Throws<UnauthorizedAccessException>(() => business.DeleteService(service.Id, "000000", salon.Admin));
        var account = salon.Read<UserAccount>(salon.Admin.Id);Assert.Equal(5, account.SuperPinFailedAttempts);Assert.True(account.SuperPinLockedUntilUtc > DateTime.UtcNow);Assert.Equal(0, account.FailedLoginAttempts);
        Assert.False(salon.Read<SalonService>(service.Id).IsDeleted);Assert.Throws<UnauthorizedAccessException>(() => business.DeleteService(service.Id, SuperPin, salon.Admin));
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();var auth = new AuthService(reopened);var session = auth.Login("administrator", TestSalon.Password);var reopenedBusiness = new BusinessService(reopened, auth);
        Assert.Throws<UnauthorizedAccessException>(() => reopenedBusiness.DeleteService(service.Id, SuperPin, session));
        auth.EnrollSuperPin("918273", TestSalon.Password, session);reopenedBusiness.DeleteService(service.Id, "918273", session);
        Assert.True(salon.Read<SalonService>(service.Id).IsDeleted);
        using var db = reopened.OpenContext();Assert.DoesNotContain(db.Set<AuditLog>().ToList(), a => a.Details.Contains(SuperPin) || a.Details.Contains("000000") || a.Details.Contains("918273"));
    }

    [Fact]
    public void EnrollmentAndDeletionRejectForgedRolesAndRequirePasswordConfirmedSeparateCredential()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);var service = Service(salon, business);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.EnrollSuperPin(SuperPin, "wrong password", salon.Admin));Assert.Null(salon.Read<UserAccount>(salon.Admin.Id).SuperPinHash);
        salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);Assert.False(salon.Auth.IsSuperPinEnabled(salon.Admin));
        Assert.Throws<UnauthorizedAccessException>(() => business.DeleteService(service.Id, "482915", salon.Admin));
        salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);Assert.True(salon.Auth.IsSuperPinEnabled(salon.Admin));
        var editable = business.List<UserAccount>(salon.Admin).Single();Assert.Null(editable.SuperPinHash);editable.SuperPinHash = "forged";editable.SuperPinFailedAttempts = 99;
        business.Save(editable, salon.Admin);Assert.NotEqual("forged", salon.Read<UserAccount>(salon.Admin.Id).SuperPinHash);Assert.Equal(0, salon.Read<UserAccount>(salon.Admin.Id).SuperPinFailedAttempts);
        var cashier = salon.Auth.CreateUser("restricted-delete-cashier", TestSalon.Password, null, RoleKind.Cashier, 0, false, salon.Admin);var session = salon.Auth.Login(cashier.Username, TestSalon.Password);session.Role = RoleKind.Administrator;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, session));
        Assert.Throws<UnauthorizedAccessException>(() => business.DeleteService(service.Id, SuperPin, session));
        Assert.False(salon.Read<SalonService>(service.Id).IsDeleted);
    }

    [Fact]
    public void PasswordChangeInvalidatesBothLoginAndSuperPinAndNoLoginEnvelopeCanBeReused()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);
        using(var db = salon.Database.OpenContext()){var account = db.Set<UserAccount>().Find(salon.Admin.Id)!;account.SuperPinHash = account.PinHash;db.SaveChanges();}
        Assert.False(salon.Auth.IsSuperPinEnabled(salon.Admin));salon.Auth.EnrollSuperPin(SuperPin, TestSalon.Password, salon.Admin);
        salon.Auth.ChangeOwnPassword(TestSalon.Password, "new-super-password-2026!", salon.Admin);
        var saved = salon.Read<UserAccount>(salon.Admin.Id);Assert.Null(saved.SuperPinHash);Assert.Null(saved.PinHash);Assert.Null(saved.PinLookupHash);Assert.Null(salon.Auth.CurrentUser);
    }
}
