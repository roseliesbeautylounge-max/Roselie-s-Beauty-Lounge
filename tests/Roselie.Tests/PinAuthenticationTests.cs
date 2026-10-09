using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class PinAuthenticationTests
{
    private const string Pin = "482915";

    [Fact]
    public void PinIsDeviceProtectedSaltedAndSurvivesReopeningWithoutExposingCredentials()
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);
        var account = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.NotNull(account.PinHash);
        Assert.DoesNotContain(Pin, account.PinHash);
        var bytes = ProtectedData.Unprotect(Convert.FromBase64String(account.PinHash), account.Id.ToByteArray(), DataProtectionScope.CurrentUser);
        try
        {
            var encoded = Encoding.UTF8.GetString(bytes);
            Assert.StartsWith("argon2id$", encoded);
            Assert.True(PasswordHasher.Verify(Pin, encoded));
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
        Assert.True(salon.Auth.IsPinEnabled(salon.Admin));
        Assert.Null(salon.Admin.PinHash);
        Assert.Null(new BusinessService(salon.Database, salon.Auth).List<UserAccount>(salon.Admin).Single().PinHash);
        var original = account.PinHash;
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);
        Assert.NotEqual(original, salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();
        var auth = new AuthService(reopened);
        var session = auth.LoginWithPin("  ADMINISTRATOR  ", Pin);
        Assert.Same(session, auth.CurrentUser);
        Assert.Equal(salon.Admin.Id, session.Id);
        Assert.Equal(RoleKind.Administrator, auth.Require(session, true).Role);
        Assert.Empty(session.PasswordHash);Assert.Null(session.RecoveryCodeHash);Assert.Null(session.PinHash);
        Assert.Equal(account.PasswordHash, salon.Read<UserAccount>(salon.Admin.Id).PasswordHash);
        Assert.Equal(account.RecoveryCodeHash, salon.Read<UserAccount>(salon.Admin.Id).RecoveryCodeHash);
        Assert.Equal(0, salon.Read<UserAccount>(salon.Admin.Id).FailedLoginAttempts);
        Assert.NotNull(salon.Read<UserAccount>(salon.Admin.Id).LastLoginAtUtc);
        using var db = salon.Database.OpenContext();
        Assert.DoesNotContain(db.Set<AuditLog>().Select(a => a.Details).ToList(), details => details.Contains(Pin) || details.Contains(TestSalon.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567890123")]
    [InlineData("abcdef")]
    [InlineData("١٢٣٤٥٦")]
    [InlineData("１２３４５６")]
    [InlineData("123 456")]
    [InlineData(" 123456")]
    public void EnrollmentOnlyAcceptsSixToTwelveAsciiDigits(string invalid)
    {
        using var salon = new TestSalon();
        Assert.Throws<ArgumentException>(() => salon.Auth.EnrollPin(invalid, TestSalon.Password, salon.Admin));
        Assert.Null(salon.Read<UserAccount>(salon.Admin.Id).PinHash);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("123456789012")]
    public void EnrollmentAcceptsBothPinLengthBoundaries(string pin)
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin(pin, TestSalon.Password, salon.Admin);
        salon.Auth.Logout();
        Assert.Equal(salon.Admin.Id, salon.Auth.LoginWithPin("administrator", pin).Id);
    }

    [Fact]
    public void EnrollmentAndRemovalRequireCurrentPasswordAndAuditFailuresWithoutSecrets()
    {
        using var salon = new TestSalon();
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.EnrollPin(Pin, "incorrect confirmation", salon.Admin));
        Assert.Null(salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        Assert.Equal(1, salon.Read<UserAccount>(salon.Admin.Id).FailedLoginAttempts);
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);
        var hash = salon.Read<UserAccount>(salon.Admin.Id).PinHash;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.DisablePin("incorrect confirmation", salon.Admin));
        Assert.Equal(hash, salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        Assert.Equal(1, salon.Read<UserAccount>(salon.Admin.Id).FailedLoginAttempts);
        using var db = salon.Database.OpenContext();
        Assert.Contains(db.Set<AuditLog>().ToList(), a => a.Action == "PIN enrollment failed");
        Assert.Contains(db.Set<AuditLog>().ToList(), a => a.Action == "PIN removal failed");
        Assert.DoesNotContain(db.Set<AuditLog>().ToList(), a => a.Details.Contains("incorrect confirmation") || a.Details.Contains(Pin));
    }

    [Fact]
    public void PinAndPasswordFailuresSharePersistedLockout()
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);salon.Auth.Logout();
        for (var i = 0; i < 3; i++)Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("administrator", "000000"));
        for (var i = 0; i < 2; i++)Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Login("administrator", "incorrect password"));
        var account = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Equal(5, account.FailedLoginAttempts);Assert.True(account.LockedUntilUtc > DateTime.UtcNow);
        var reopened = new AuthService(salon.Database);
        Assert.Throws<UnauthorizedAccessException>(() => reopened.LoginWithPin("administrator", Pin));
        Assert.Throws<UnauthorizedAccessException>(() => reopened.Login("administrator", TestSalon.Password));
        Assert.Null(reopened.CurrentUser);
    }

    [Fact]
    public void DisabledPinCannotSignInAndPasswordRemainsAvailable()
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);
        var session = salon.Auth.LoginWithPin("administrator", Pin);
        salon.Auth.DisablePin(TestSalon.Password, session);
        Assert.False(salon.Auth.IsPinEnabled(session));Assert.Null(salon.Read<UserAccount>(session.Id).PinHash);
        salon.Auth.Logout();
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("administrator", Pin));
        Assert.Equal(session.Id, salon.Auth.Login("administrator", TestSalon.Password).Id);
    }

    [Fact]
    public void EnrollmentRejectsForgedStaleForeignAndExpiredSessions()
    {
        using var salon = new TestSalon();
        var clone = new UserAccount { Id = salon.Admin.Id, Username = salon.Admin.Username, Role = RoleKind.Administrator };
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.EnrollPin(Pin, TestSalon.Password, clone));
        var cashier = salon.Auth.CreateUser("pin-cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var session = salon.Auth.Login("pin-cashier", TestSalon.Password);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin));
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.EnrollPin(Pin, TestSalon.Password, cashier));
        salon.Auth.EnrollPin(Pin, TestSalon.Password, session);
        Assert.Null(salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        Assert.NotNull(salon.Read<UserAccount>(cashier.Id).PinHash);
        salon.Auth.SessionTimeout = TimeSpan.FromTicks(-1);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.DisablePin(TestSalon.Password, session));
    }

    [Fact]
    public void PinSessionUsesStoredRoleAndImmutableSessionIdentity()
    {
        using var salon = new TestSalon();
        var cashier = salon.Auth.CreateUser("pin-cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var session = salon.Auth.Login("pin-cashier", TestSalon.Password);
        salon.Auth.EnrollPin(Pin, TestSalon.Password, session);salon.Auth.Logout();
        var pinSession = salon.Auth.LoginWithPin("pin-cashier", Pin);
        Assert.Equal(RoleKind.Cashier, salon.Auth.Require(pinSession).Role);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Require(new UserAccount { Id = cashier.Id }, true));
        pinSession.Role = RoleKind.Administrator;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Require(pinSession, true));
        pinSession.Id = salon.Admin.Id;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.EnrollPin("123456", TestSalon.Password, pinSession));
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.DisablePin(TestSalon.Password, pinSession));
    }

    [Fact]
    public void GenericAccountSaveCannotReplaceOrRemovePinAndReturnsNoCredentialHash()
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);
        var business = new BusinessService(salon.Database, salon.Auth);var hash = salon.Read<UserAccount>(salon.Admin.Id).PinHash;
        var editable = business.List<UserAccount>(salon.Admin).Single();editable.PinHash = "forged-pin-envelope";
        var result = business.Save(editable, salon.Admin);
        Assert.Null(result.PinHash);Assert.Empty(result.PasswordHash);Assert.Null(result.RecoveryCodeHash);
        Assert.Equal(hash, salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        result.PinHash = null;business.Save(result, salon.Admin);
        Assert.Equal(hash, salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        Assert.Equal(salon.Admin.Id, salon.Auth.LoginWithPin("administrator", Pin).Id);
    }

    [Fact]
    public void TamperedOrForeignAccountPinEnvelopeIsUnavailableAndCanBeReenrolledWithPassword()
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);
        var cashier = salon.Auth.CreateUser("pin-cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        using (var db = salon.Database.OpenContext())
        {
            var account = db.Set<UserAccount>().Find(cashier.Id)!;
            account.PinHash = salon.Read<UserAccount>(salon.Admin.Id).PinHash;db.SaveChanges();
        }
        var session = salon.Auth.Login("pin-cashier", TestSalon.Password);
        Assert.False(salon.Auth.IsPinEnabled(session));
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("pin-cashier", Pin));
        using (var db = salon.Database.OpenContext()) {db.Set<UserAccount>().Find(cashier.Id)!.PinHash = "corrupt envelope";db.SaveChanges();}
        Assert.False(salon.Auth.IsPinEnabled(session));
        salon.Auth.EnrollPin("918273", TestSalon.Password, session);
        Assert.True(salon.Auth.IsPinEnabled(session));
        Assert.Equal(cashier.Id, salon.Auth.LoginWithPin("pin-cashier", "918273").Id);
    }

    [Fact]
    public void InactiveAccountsRejectPinLoginAndEnrollment()
    {
        using var salon = new TestSalon();
        var cashier = salon.Auth.CreateUser("pin-cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var session = salon.Auth.Login("pin-cashier", TestSalon.Password);salon.Auth.EnrollPin(Pin, TestSalon.Password, session);
        using (var db = salon.Database.OpenContext()) {db.Set<UserAccount>().Find(cashier.Id)!.IsActive = false;db.SaveChanges();}
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.DisablePin(TestSalon.Password, session));
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("pin-cashier", Pin));
        Assert.Null(salon.Auth.CurrentUser);
    }

    [Fact]
    public void RecoveryAndAdministrativePasswordResetInvalidateTheOldPin()
    {
        using var salon = new TestSalon();
        salon.Auth.EnrollPin(Pin, TestSalon.Password, salon.Admin);
        const string replacement = "pin-recovery-password-2026!";
        salon.Auth.RecoverAccount("administrator", salon.RecoveryCode, replacement);
        Assert.Null(salon.Read<UserAccount>(salon.Admin.Id).PinHash);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("administrator", Pin));
        var session = salon.Auth.Login("administrator", replacement);salon.Auth.EnrollPin(Pin, replacement, session);
        salon.Auth.ResetPassword(session.Id, TestSalon.Password, session);
        Assert.Null(salon.Auth.CurrentUser);Assert.Null(salon.Read<UserAccount>(session.Id).PinHash);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("administrator", Pin));
        Assert.Equal(session.Id, salon.Auth.Login("administrator", TestSalon.Password).Id);
    }

    [Fact]
    public void PrePinEncryptedSchemaGainsNullableColumnWithoutChangingAccountsOrPassword()
    {
        using var salon = new TestSalon();
        var original = salon.Read<UserAccount>(salon.Admin.Id);
        using (var db = salon.Database.OpenContext()) db.Database.ExecuteSqlRaw("ALTER TABLE \"UserAccount\" DROP COLUMN \"PinHash\";");
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();
        var auth = new AuthService(reopened);var session = auth.Login("administrator", TestSalon.Password);
        Assert.Equal(original.Id, session.Id);Assert.False(auth.IsPinEnabled(session));
        Assert.Equal(original.PasswordHash, salon.Read<UserAccount>(original.Id).PasswordHash);
        Assert.Equal(original.RecoveryCodeHash, salon.Read<UserAccount>(original.Id).RecoveryCodeHash);
        Assert.True(reopened.CheckIntegrity());
        auth.EnrollPin(Pin, TestSalon.Password, session);
        Assert.Equal(original.Id, auth.LoginWithPin("administrator", Pin).Id);
    }
}
