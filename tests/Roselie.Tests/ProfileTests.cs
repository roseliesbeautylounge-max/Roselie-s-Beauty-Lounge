using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class ProfileTests
{
    [Fact]
    public void ProfileUpdateTrimsAndPersistsNameWithoutChangingUsernameOrCredentials()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var original = salon.Read<UserAccount>(salon.Admin.Id);
        salon.Auth.UpdateOwnProfile("  Roselie Dela Cruz  ", salon.Admin);
        var saved = salon.Read<UserAccount>(original.Id);
        Assert.Equal("Roselie Dela Cruz", saved.DisplayName);Assert.Equal(saved.DisplayName, salon.Admin.DisplayName);
        Assert.Same(salon.Admin, salon.Auth.CurrentUser);Assert.Equal(saved.DisplayName, salon.Auth.Require(salon.Admin, true).DisplayName);
        Assert.Equal(original.Id, saved.Id);Assert.Equal(original.CreatedAtUtc, saved.CreatedAtUtc);
        Assert.Equal(original.Username, saved.Username);Assert.Equal(original.NormalizedUsername, saved.NormalizedUsername);
        Assert.Equal(original.Role, saved.Role);Assert.Equal(original.RoleId, saved.RoleId);
        Assert.Equal(original.PasswordHash, saved.PasswordHash);Assert.Equal(original.RecoveryCodeHash, saved.RecoveryCodeHash);Assert.Equal(original.PinHash, saved.PinHash);
        Assert.Equal(original.PinLookupHash, saved.PinLookupHash);
        Assert.Empty(salon.Admin.PasswordHash);Assert.Null(salon.Admin.RecoveryCodeHash);Assert.Null(salon.Admin.PinHash);
        Assert.True(salon.Auth.IsPinEnabled(salon.Admin));
        using(var db = salon.Database.OpenContext())
        {
            var audit = Assert.Single(db.Set<AuditLog>().Where(a => a.Action == "Administrator profile updated").ToList());
            Assert.Equal(saved.Id, audit.UserId);Assert.Equal(saved.Id, audit.EntityId);
            Assert.DoesNotContain(TestSalon.Password, audit.Details);Assert.DoesNotContain("482915", audit.Details);
        }
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();var auth = new AuthService(reopened);
        var session = auth.Login("administrator", TestSalon.Password);Assert.Equal("Roselie Dela Cruz", session.DisplayName);Assert.Equal(saved.Id, session.Id);
        var pinSession = auth.LoginWithPin("administrator", "482915");Assert.Equal("Roselie Dela Cruz", pinSession.DisplayName);
        const string replacement = "profile-password-change-2026!";
        auth.ChangeOwnPassword(TestSalon.Password, replacement, pinSession);
        var second = new DatabaseService(salon.DirectoryPath);second.Initialize();var secondAuth = new AuthService(second);
        Assert.Equal("Roselie Dela Cruz", secondAuth.Login("administrator", replacement).DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Owner\nName")]
    [InlineData("\tOwner")]
    [InlineData("Owner\r")]
    [InlineData("Owner\u0085Name")]
    public void InvalidNamesDoNotModifyProfileCredentialsOrAudit(string? invalid)
    {
        using var salon = new TestSalon();salon.Auth.UpdateOwnProfile("Original owner", salon.Admin);
        var original = salon.Read<UserAccount>(salon.Admin.Id);var auditCount = salon.Count<AuditLog>();
        Assert.Throws<ArgumentException>(() => salon.Auth.UpdateOwnProfile(invalid!, salon.Admin));
        var saved = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Equal(original.DisplayName, saved.DisplayName);Assert.Equal(original.DisplayName, salon.Admin.DisplayName);
        Assert.Equal(original.PasswordHash, saved.PasswordHash);Assert.Equal(original.RecoveryCodeHash, saved.RecoveryCodeHash);
        Assert.Equal(0, saved.FailedLoginAttempts);Assert.Equal(auditCount, salon.Count<AuditLog>());Assert.Same(salon.Admin, salon.Auth.CurrentUser);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void ProfileNameAcceptsBothLengthBoundaries(int length)
    {
        using var salon = new TestSalon();var name = new string('x', length);
        salon.Auth.UpdateOwnProfile(name, salon.Admin);
        Assert.Equal(name, salon.Read<UserAccount>(salon.Admin.Id).DisplayName);Assert.Equal(name, salon.Admin.DisplayName);
    }

    [Fact]
    public void ProfileNameRejectsMoreThanOneHundredCharactersAfterTrimming()
    {
        using var salon = new TestSalon();
        Assert.Throws<ArgumentException>(() => salon.Auth.UpdateOwnProfile("  " + new string('x', 101) + "  ", salon.Admin));
        Assert.Empty(salon.Read<UserAccount>(salon.Admin.Id).DisplayName);Assert.Empty(salon.Admin.DisplayName);
    }

    [Theory]
    [InlineData(RoleKind.Cashier)]
    [InlineData(RoleKind.SalonStaff)]
    public void ProfileUpdateCannotBeEnabledBySpoofingClientAdministratorRole(RoleKind role)
    {
        using var salon = new TestSalon();
        var account = salon.Auth.CreateUser("restricted-profile-account", TestSalon.Password, null, role, 0, false, salon.Admin);
        var session = salon.Auth.Login(account.Username, TestSalon.Password);session.Role = RoleKind.Administrator;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Unauthorized name", session));
        Assert.Empty(salon.Read<UserAccount>(account.Id).DisplayName);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Stale owner name", salon.Admin));
    }

    [Fact]
    public void ProfileUpdateRejectsForgedMutatedForeignAndExpiredSessionReferences()
    {
        using var salon = new TestSalon();var id = salon.Admin.Id;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Forged name", new UserAccount { Id = id, Role = RoleKind.Administrator }));
        salon.Admin.Id = Guid.NewGuid();Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Mutated identity", salon.Admin));salon.Admin.Id = id;
        var account = salon.Auth.CreateUser("other-profile-owner", TestSalon.Password, null, RoleKind.Administrator, 100, true, salon.Admin);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Foreign name", account));
        var current = salon.Auth.Login(account.Username, TestSalon.Password);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Stale name", salon.Admin));
        salon.Auth.SessionTimeout = TimeSpan.FromTicks(-1);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Expired name", current));
        Assert.Null(salon.Auth.CurrentUser);Assert.Empty(salon.Read<UserAccount>(id).DisplayName);Assert.Empty(salon.Read<UserAccount>(account.Id).DisplayName);
    }

    [Fact]
    public void ProfileUpdateRechecksPersistedRoleAndActiveStatus()
    {
        using var salon = new TestSalon();
        using(var db = salon.Database.OpenContext())
        {
            var account = db.Set<UserAccount>().Find(salon.Admin.Id)!;
            account.Role = RoleKind.Cashier;account.RoleId = db.Set<Role>().Single(r => r.Kind == RoleKind.Cashier).Id;db.SaveChanges();
        }
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Demoted administrator", salon.Admin));
        using(var db = salon.Database.OpenContext()) {db.Set<UserAccount>().Find(salon.Admin.Id)!.IsActive = false;db.SaveChanges();}
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.UpdateOwnProfile("Inactive account", salon.Admin));
        Assert.Empty(salon.Read<UserAccount>(salon.Admin.Id).DisplayName);Assert.Null(salon.Auth.CurrentUser);
    }

    [Fact]
    public void PreProfileDatabaseUpgradePreservesAccountAndUsesEmptyUsernameFallback()
    {
        using var salon = new TestSalon();var original = salon.Read<UserAccount>(salon.Admin.Id);
        using(var db = salon.Database.OpenContext())db.Database.ExecuteSqlRaw("ALTER TABLE \"UserAccount\" DROP COLUMN \"DisplayName\";");
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();Assert.True(reopened.CheckIntegrity());
        var auth = new AuthService(reopened);var session = auth.Login("administrator", TestSalon.Password);
        Assert.Equal(original.Id, session.Id);Assert.Empty(session.DisplayName);Assert.Equal(original.Username, session.Username);
        var saved = salon.Read<UserAccount>(original.Id);
        Assert.Equal(original.PasswordHash, saved.PasswordHash);Assert.Equal(original.RecoveryCodeHash, saved.RecoveryCodeHash);
        auth.UpdateOwnProfile("Owner after upgrade", session);
        Assert.Equal("Owner after upgrade", salon.Read<UserAccount>(original.Id).DisplayName);
    }
}
