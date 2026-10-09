using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class PinOnlyLoginTests
{
    [Fact]
    public void PinAloneIdentifiesUniqueActiveAccountAndKeepsCredentialsPrivate()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var cashier = salon.Auth.CreateUser("numpad-cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var cashierSession = salon.Auth.Login(cashier.Username, TestSalon.Password);salon.Auth.EnrollPin("918273", TestSalon.Password, cashierSession);salon.Auth.Logout();
        var signedIn = salon.Auth.LoginWithPin("918273");
        Assert.Equal(cashier.Id, signedIn.Id);Assert.Equal(RoleKind.Cashier, salon.Auth.Require(signedIn).Role);
        Assert.Same(signedIn, salon.Auth.CurrentUser);Assert.Null(signedIn.PinHash);Assert.Null(signedIn.PinLookupHash);Assert.Null(signedIn.SuperPinHash);
        Assert.Equal(salon.Admin.Id, salon.Auth.LoginWithPin("482915").Id);
        var business = new BusinessService(salon.Database, salon.Auth);
        Assert.All(business.List<UserAccount>(salon.Auth.CurrentUser!), user => Assert.Null(user.PinLookupHash));
        var lookup = salon.Read<UserAccount>(cashier.Id).PinLookupHash;Assert.StartsWith("v1:", lookup);Assert.DoesNotContain("918273", lookup);
        var protectedKey = File.ReadAllBytes(Path.Combine(salon.DirectoryPath, "pin-lookup.key"));
        var key = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
        try {Assert.Equal(32, key.Length);Assert.NotEqual(key, protectedKey);}
        finally {CryptographicOperations.ZeroMemory(key);}
    }

    [Fact]
    public void EnrollmentRejectsAnotherActiveAccountsPinWithoutChangingEitherAccount()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var original = salon.Read<UserAccount>(salon.Admin.Id);
        var cashier = salon.Auth.CreateUser("unique-pin-cashier", TestSalon.Password, null, RoleKind.Cashier, 0, false, salon.Admin);
        var session = salon.Auth.Login(cashier.Username, TestSalon.Password);
        Assert.Throws<ArgumentException>(() => salon.Auth.EnrollPin("482915", TestSalon.Password, session));
        Assert.Null(salon.Read<UserAccount>(cashier.Id).PinHash);Assert.Null(salon.Read<UserAccount>(cashier.Id).PinLookupHash);
        Assert.Equal(original.PinHash, salon.Read<UserAccount>(original.Id).PinHash);Assert.Equal(original.PinLookupHash, salon.Read<UserAccount>(original.Id).PinLookupHash);
    }

    [Fact]
    public void UnknownPinsPersistGlobalThrottleAndPasswordLoginRemainsAvailable()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);salon.Auth.Logout();
        for(var i = 0; i < 5; i++)Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("000000"));
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();var auth = new AuthService(reopened);
        Assert.Throws<UnauthorizedAccessException>(() => auth.LoginWithPin("482915"));Assert.Null(auth.CurrentUser);
        var session = auth.Login("administrator", TestSalon.Password);Assert.Equal(salon.Admin.Id, session.Id);
        auth.EnrollPin("482915", TestSalon.Password, session);auth.Logout();
        Assert.Equal(session.Id, auth.LoginWithPin("482915").Id);
        using var db = reopened.OpenContext();
        Assert.DoesNotContain(db.Set<AuditLog>().ToList(), a => a.Details.Contains("482915") || a.Details.Contains("000000"));
    }

    [Fact]
    public void DisabledAndInactiveAccountsCannotBeSelectedByPinAlone()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var cashier = salon.Auth.CreateUser("inactive-pin-cashier", TestSalon.Password, null, RoleKind.Cashier, 0, false, salon.Admin);
        var session = salon.Auth.Login(cashier.Username, TestSalon.Password);salon.Auth.EnrollPin("918273", TestSalon.Password, session);
        using(var db = salon.Database.OpenContext()){db.Set<UserAccount>().Find(cashier.Id)!.IsActive = false;db.SaveChanges();}
        salon.Auth.Logout();Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("918273"));
        var owner = salon.Auth.LoginWithPin("482915");salon.Auth.DisablePin(TestSalon.Password, owner);salon.Auth.Logout();
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("482915"));
        Assert.Equal(owner.Id, salon.Auth.Login("administrator", TestSalon.Password).Id);
    }

    [Fact]
    public void AmbiguousLegacyPinsFailWithoutSelectingAnAccount()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var cashier = salon.Auth.CreateUser("legacy-duplicate-pin", TestSalon.Password, null, RoleKind.Cashier, 0, false, salon.Admin);
        var bytes = Encoding.UTF8.GetBytes(PasswordHasher.Hash("482915"));
        try
        {
            using var db = salon.Database.OpenContext();var account = db.Set<UserAccount>().Find(cashier.Id)!;
            account.PinHash = Convert.ToBase64String(ProtectedData.Protect(bytes, account.Id.ToByteArray(), DataProtectionScope.CurrentUser));account.PinLookupHash = null;db.SaveChanges();
        }
        finally {CryptographicOperations.ZeroMemory(bytes);}
        salon.Auth.Logout();Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("482915"));Assert.Null(salon.Auth.CurrentUser);
        Assert.Equal(cashier.Id, salon.Auth.Login(cashier.Username, TestSalon.Password).Id);
    }

    [Fact]
    public void MissingOrRecreatedLookupKeyRebuildsStaleNonemptyHashesAcrossReopen()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        var originalLookup = salon.Read<UserAccount>(salon.Admin.Id).PinLookupHash;
        File.Delete(Path.Combine(salon.DirectoryPath, "pin-lookup.key"));
        var reopened = new DatabaseService(salon.DirectoryPath);reopened.Initialize();var auth = new AuthService(reopened);
        Assert.Equal(salon.Admin.Id, auth.LoginWithPin("482915").Id);
        var rebuilt = salon.Read<UserAccount>(salon.Admin.Id).PinLookupHash;Assert.NotEqual(originalLookup, rebuilt);
        var key = RandomNumberGenerator.GetBytes(32);
        try {File.WriteAllBytes(Path.Combine(salon.DirectoryPath, "pin-lookup.key"), ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser));}
        finally {CryptographicOperations.ZeroMemory(key);}
        var second = new DatabaseService(salon.DirectoryPath);second.Initialize();var secondAuth = new AuthService(second);
        Assert.Equal(salon.Admin.Id, secondAuth.LoginWithPin("482915").Id);Assert.NotEqual(rebuilt, salon.Read<UserAccount>(salon.Admin.Id).PinLookupHash);
    }

    [Fact]
    public void UnreadableDeviceHashDoesNotAuthenticateByLookupAlone()
    {
        using var salon = new TestSalon();salon.Auth.EnrollPin("482915", TestSalon.Password, salon.Admin);
        using(var db = salon.Database.OpenContext()){db.Set<UserAccount>().Find(salon.Admin.Id)!.PinHash = "unreadable-on-this-Windows-profile";db.SaveChanges();}
        salon.Auth.Logout();Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.LoginWithPin("482915"));
        var session = salon.Auth.Login("administrator", TestSalon.Password);Assert.False(salon.Auth.IsPinEnabled(session));
        salon.Auth.EnrollPin("482915", TestSalon.Password, session);Assert.Equal(session.Id, salon.Auth.LoginWithPin("482915").Id);
    }
}
