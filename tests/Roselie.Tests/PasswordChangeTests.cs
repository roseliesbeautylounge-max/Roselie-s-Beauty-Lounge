using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class PasswordChangeTests
{
    private const string Replacement = "new-owner-password-2026!";

    [Fact]
    public void AdministratorChangesOwnPasswordAndCommitEndsSessionAndDisablesPin()
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var original = salon.Read<UserAccount>(salon.Admin.Id);
        salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, salon.Admin);
        var saved = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.NotEqual(original.PasswordHash, saved.PasswordHash);
        Assert.True(PasswordHasher.Verify(Replacement, saved.PasswordHash));
        Assert.False(PasswordHasher.Verify(TestSalon.Password, saved.PasswordHash));
        Assert.Equal(original.RecoveryCodeHash, saved.RecoveryCodeHash);
        Assert.Null(saved.PinHash);Assert.Equal(0, saved.FailedLoginAttempts);Assert.Null(saved.LockedUntilUtc);
        Assert.Null(salon.Auth.CurrentUser);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Require(salon.Admin));
        using (var db = salon.Database.OpenContext())
        {
            var audit = Assert.Single(db.Set<AuditLog>().Where(a => a.Action == "Administrator password changed").ToList());
            Assert.Equal(saved.Id, audit.UserId);Assert.Equal(saved.Id, audit.EntityId);
            Assert.DoesNotContain(Replacement, audit.Details);Assert.DoesNotContain(TestSalon.Password, audit.Details);
        }
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();var auth = new AuthService(reopened);
        Assert.Throws<UnauthorizedAccessException>(() => auth.Login("administrator", TestSalon.Password));
        Assert.Throws<UnauthorizedAccessException>(() => auth.LoginWithPin("administrator", "482915"));
        Assert.Equal(saved.Id, auth.Login("administrator", Replacement).Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(257)]
    public void InvalidNewPasswordLeavesCredentialsAndSessionUnchanged(int length)
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var original = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Throws<ArgumentException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, new string('x', length), salon.Admin));
        var saved = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Equal(original.PasswordHash, saved.PasswordHash);Assert.Equal(original.PinHash, saved.PinHash);
        Assert.Equal(0, saved.FailedLoginAttempts);Assert.Same(salon.Admin, salon.Auth.CurrentUser);
        Assert.Equal(RoleKind.Administrator, salon.Auth.Require(salon.Admin, true).Role);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(256)]
    public void PasswordChangeAcceptsBothSupportedLengthBoundaries(int length)
    {
        using var salon = new TestSalon();var password = new string('x', length);
        salon.Auth.ChangeOwnPassword(TestSalon.Password, password, salon.Admin);
        Assert.Null(salon.Auth.CurrentUser);
        Assert.Equal(salon.Admin.Id, new AuthService(salon.Database).Login("administrator", password).Id);
    }

    [Fact]
    public void IncorrectConfirmationIsAuditedAndPreservesPasswordPinAndCurrentSession()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var original = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword("incorrect-current-password", Replacement, salon.Admin));
        var saved = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Equal(original.PasswordHash, saved.PasswordHash);Assert.Equal(original.PinHash, saved.PinHash);
        Assert.Equal(1, saved.FailedLoginAttempts);Assert.Same(salon.Admin, salon.Auth.CurrentUser);
        using var db = salon.Database.OpenContext();
        var audit = Assert.Single(db.Set<AuditLog>().Where(a => a.Action == "Password change failed").ToList());
        Assert.DoesNotContain("incorrect-current-password", audit.Details);Assert.DoesNotContain(Replacement, audit.Details);
        Assert.Empty(db.Set<AuditLog>().Where(a => a.Action == "Administrator password changed").ToList());
    }

    [Fact]
    public void PasswordChangeConfirmationSharesPinAndPasswordLockoutAcrossReopening()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var original = salon.Read<UserAccount>(salon.Admin.Id);
        for(var i = 0; i < 2; i++)Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("administrator", "000000"));
        for(var i = 0; i < 2; i++)Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword("incorrect password", Replacement, salon.Admin));
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Login("administrator", "incorrect password"));
        var saved = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Equal(5, saved.FailedLoginAttempts);Assert.True(saved.LockedUntilUtc > DateTime.UtcNow);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, salon.Admin));
        Assert.Equal(original.PasswordHash, salon.Read<UserAccount>(salon.Admin.Id).PasswordHash);
        Assert.Equal(original.PinHash, salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();var auth = new AuthService(reopened);
        Assert.Throws<UnauthorizedAccessException>(() => auth.Login("administrator", TestSalon.Password));
        Assert.Throws<UnauthorizedAccessException>(() => auth.LoginWithPin("administrator", "482915"));
        Assert.Null(auth.CurrentUser);
    }

    [Theory]
    [InlineData(RoleKind.Cashier)]
    [InlineData(RoleKind.SalonStaff)]
    public void NonAdministratorCannotChangePasswordBySpoofingClientRole(RoleKind role)
    {
        using var salon = new TestSalon();
        var account = salon.Auth.CreateUser("restricted-account", TestSalon.Password, null, role, 0, false, salon.Admin);
        var session = salon.Auth.Login(account.Username, TestSalon.Password);var original = salon.Read<UserAccount>(account.Id);
        session.Role = RoleKind.Administrator;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, session));
        Assert.Equal(original.PasswordHash, salon.Read<UserAccount>(account.Id).PasswordHash);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, salon.Admin));
    }

    [Fact]
    public void PasswordChangeRejectsForgedMutatedAndExpiredSessions()
    {
        using var salon = new TestSalon();var id = salon.Admin.Id;var original = salon.Read<UserAccount>(id);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, new UserAccount { Id = id, Role = RoleKind.Administrator }));
        salon.Admin.Id = Guid.NewGuid();
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, salon.Admin));
        salon.Admin.Id = id;salon.Auth.SessionTimeout = TimeSpan.FromTicks(-1);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, salon.Admin));
        Assert.Equal(original.PasswordHash, salon.Read<UserAccount>(id).PasswordHash);Assert.Null(salon.Auth.CurrentUser);
    }

    [Fact]
    public void PasswordChangeChecksPersistedAdministratorRoleAndActiveStatus()
    {
        using var salon = new TestSalon();var original = salon.Read<UserAccount>(salon.Admin.Id);
        using(var db = salon.Database.OpenContext())
        {
            var account = db.Set<UserAccount>().Find(salon.Admin.Id)!;
            account.Role = RoleKind.Cashier;account.RoleId = db.Set<Role>().Single(r => r.Kind == RoleKind.Cashier).Id;db.SaveChanges();
        }
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, salon.Admin));
        using(var db = salon.Database.OpenContext()) {db.Set<UserAccount>().Find(salon.Admin.Id)!.IsActive = false;db.SaveChanges();}
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.ChangeOwnPassword(TestSalon.Password, Replacement, salon.Admin));
        Assert.Equal(original.PasswordHash, salon.Read<UserAccount>(salon.Admin.Id).PasswordHash);Assert.Null(salon.Auth.CurrentUser);
    }

    [Fact]
    public async Task ConcurrentAccountCreationOnlyLinksOneAccountToAnEmployee()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);
        var employee = business.Save(new Employee { FullName = "Isolated account-link employee", Position = "Stylist" }, salon.Admin);
        async Task<(UserAccount? Account, Exception? Error)> Attempt(string username) => await Task.Run(() =>
        {
            try {return (salon.Auth.CreateUser(username, TestSalon.Password, employee.Id, RoleKind.Cashier, 10, false, salon.Admin), (Exception?)null);}
            catch(Exception error) {return ((UserAccount?)null, error);}
        });
        var results = await Task.WhenAll(Attempt("linked-account-one"), Attempt("linked-account-two"));
        Assert.NotNull(Assert.Single(results.Where(r => r.Error == null)).Account);
        Assert.IsType<ArgumentException>(Assert.Single(results.Where(r => r.Error != null)).Error);
        using var db = salon.Database.OpenContext();Assert.Single(db.Set<UserAccount>().Where(a => a.EmployeeId == employee.Id).ToList());
    }

    [Fact]
    public void ExistingInactiveOrReverseLinkedEmployeeAccountsCannotBeDuplicated()
    {
        using var salon = new TestSalon();var business = new BusinessService(salon.Database, salon.Auth);
        var employee = business.Save(new Employee { FullName = "Isolated linked employee" }, salon.Admin);
        var account = salon.Auth.CreateUser("original-employee-account", TestSalon.Password, employee.Id, RoleKind.Cashier, 10, false, salon.Admin);
        var editable = business.List<UserAccount>(salon.Admin).Single(a => a.Id == account.Id);editable.IsActive = false;business.Save(editable, salon.Admin);
        Assert.Throws<ArgumentException>(() => salon.Auth.CreateUser("duplicate-inactive-link", TestSalon.Password, employee.Id, RoleKind.Cashier, 10, false, salon.Admin));
        var reverse = business.Save(new Employee { FullName = "Isolated reverse-linked employee", UserAccountId = salon.Admin.Id }, salon.Admin);
        Assert.Throws<ArgumentException>(() => salon.Auth.CreateUser("duplicate-reverse-link", TestSalon.Password, reverse.Id, RoleKind.Cashier, 10, false, salon.Admin));
        Assert.Equal(account.Id, salon.Read<UserAccount>(account.Id).Id);Assert.False(salon.Read<UserAccount>(account.Id).IsActive);
        Assert.Equal(2, salon.Count<UserAccount>());
    }
}
