using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Finance;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Roselie.Tests;

public sealed class TestSalon : IDisposable
{
    private readonly string _testRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Roselie.Tests"));
    public string DirectoryPath { get; }
    public DatabaseService Database { get; }
    public AuthService Auth { get; }
    public UserAccount Admin { get; }
    public string RecoveryCode { get; }
    public const string Password = "test-only-password-2026!";

    public TestSalon()
    {
        DirectoryPath = Path.GetFullPath(Path.Combine(_testRoot, Guid.NewGuid().ToString("N")));
        Database = new DatabaseService(DirectoryPath);
        Database.Initialize();
        Auth = new AuthService(Database);
        RecoveryCode = Auth.CreateFirstAdmin("administrator", Password);
        Admin = Auth.Login("administrator", Password);
    }

    public T Read<T>(Guid id) where T : Entity
    {
        using var db = Database.OpenContext();
        return db.Set<T>().AsNoTracking().Single(x => x.Id == id);
    }

    public int Count<T>() where T : Entity
    {
        using var db = Database.OpenContext();
        return db.Set<T>().Count();
    }

    public void Dispose()
    {
        // These are isolated, generated test databases. Never delete a user data directory.
        if (DirectoryPath.StartsWith(_testRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            System.IO.Directory.Exists(DirectoryPath))
            System.IO.Directory.Delete(DirectoryPath, recursive: true);
    }
}

public sealed class AuthenticationAndDatabaseTests
{
    [Fact]
    public void LegacyEncryptedSchemaUpgradesWithoutLosingExistingStockOrCosts()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Roselie.LegacyTests"));
        var directory = Path.GetFullPath(Path.Combine(root, Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            _ = new DatabaseService(directory); // Initializes the SQLCipher provider before creating the legacy fixture.
            var key = RandomNumberGenerator.GetBytes(32);
            File.WriteAllBytes(Path.Combine(directory, "database.key"),
                ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser));
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(directory, "salon.db"), Password = Convert.ToHexString(key), Pooling = false
            }.ToString());
            connection.Open();
            using (var original = new SalonDbContext(new DbContextOptionsBuilder<SalonDbContext>().UseSqlite(connection, true).Options))
            {
                var oldSchema = Regex.Replace(original.Database.GenerateCreateScript(),
                    "(?m)^\\s*\"(?:InventoryValue|ValueChange|PriceRuleId)\"[^\\r\\n]*,\\r?\\n", "");
                original.Database.ExecuteSqlRaw(oldSchema);
                var productId = Guid.NewGuid().ToString().ToUpperInvariant();
                var averageId = Guid.NewGuid().ToString().ToUpperInvariant();
                var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                original.Database.ExecuteSqlRaw("""
                    INSERT INTO Product (Id,CreatedAtUtc,Name,Sku,Barcode,Brand,PurchaseCost,SellingPrice,StockUnit,
                        StockQuantity,ReorderLevel,AverageCost,ImagePath,IsRetail,IsActive,CostInformationComplete)
                    VALUES ({0},{1},'Legacy existing stock','LEGACY-STOCK','','',10000000,10000,'unit',
                        3000000,0,13333333,'',1,1,1)
                    """, productId, now);
                original.Database.ExecuteSqlRaw("""
                    INSERT INTO InventoryAverageCost (Id,CreatedAtUtc,ProductId,Quantity,UnitCost,UpdatedAtUtc)
                    VALUES ({0},{1},{2},3000000,13333333,{1})
                    """, averageId, now, productId);
            }
            var database = new DatabaseService(directory);
            database.Initialize();
            Assert.True(database.CheckIntegrity());
            using var reopened = database.OpenContext();
            Assert.Equal(40, reopened.Set<SalonService>().Count());
            Assert.Empty(reopened.Set<UserAccount>());
            var product = Assert.Single(reopened.Set<Product>().ToList());
            Assert.Equal("Legacy existing stock", product.Name);
            Assert.Equal(3m, product.StockQuantity);
            Assert.Equal(13.333333m, product.AverageCost);
            Assert.Equal(40m, product.InventoryValue);
            Assert.Equal(40m, Assert.Single(reopened.Set<InventoryAverageCost>().ToList()).InventoryValue);
            Assert.Empty(reopened.Set<InventoryMovement>().ToList());
            Assert.Empty(reopened.Set<SaleItem>().ToList());
        }
        finally
        {
            if (directory.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                System.IO.Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void EncryptedDatabaseRejectsPlainSqliteAndDoesNotExposeHeaderOrPassword()
    {
        using var salon = new TestSalon();
        Assert.True(salon.Database.CheckIntegrity());
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = salon.Database.DatabasePath, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master";
        Assert.Throws<SqliteException>(() => command.ExecuteScalar());
        connection.Close();
        var bytes = File.ReadAllBytes(salon.Database.DatabasePath);
        Assert.NotEqual("SQLite format 3\0", Encoding.ASCII.GetString(bytes.AsSpan(0, 16)));
        Assert.DoesNotContain(TestSalon.Password, Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void AccountsAndCatalogSurviveReopeningWithoutDuplicatingSeeds()
    {
        using var salon = new TestSalon();
        var reopened = new DatabaseService(salon.DirectoryPath);
        reopened.Initialize();
        var auth = new AuthService(reopened);
        Assert.False(auth.FirstRunRequired);
        Assert.Equal(salon.Admin.Id, auth.Login("  ADMINISTRATOR  ", TestSalon.Password).Id);
        Assert.Equal(40, salon.Count<SalonService>());
        Assert.Equal(21, salon.Count<PackageOffer>());
        Assert.Equal(0, salon.Count<Customer>());
        Assert.Equal(0, salon.Count<Product>());
        Assert.Equal(0, salon.Count<Sale>());
        var account = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.StartsWith("argon2id$", account.PasswordHash);
        Assert.NotEqual(TestSalon.Password, account.PasswordHash);
        Assert.Empty(salon.Admin.PasswordHash);
    }

    [Fact]
    public void FiveFailuresPersistLockoutAndPreventCorrectPasswordLogin()
    {
        using var salon = new TestSalon();
        salon.Auth.Logout();
        for (var i = 0; i < 5; i++)
            Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Login("administrator", "wrong-password"));
        var account = salon.Read<UserAccount>(salon.Admin.Id);
        Assert.Equal(5, account.FailedLoginAttempts);
        Assert.True(account.LockedUntilUtc > DateTime.UtcNow);
        Assert.Throws<UnauthorizedAccessException>(() => new AuthService(salon.Database).Login("administrator", TestSalon.Password));
    }

    [Fact]
    public void RecoveryResetsPasswordRotatesCodeAndInvalidatesOldSession()
    {
        using var salon = new TestSalon();
        const string replacement = "replacement-password-2026!";
        var code = salon.Auth.RecoverAccount("administrator", salon.RecoveryCode, replacement);
        Assert.NotEqual(salon.RecoveryCode, code);
        Assert.Null(salon.Auth.CurrentUser);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Login("administrator", TestSalon.Password));
        Assert.Equal(salon.Admin.Id, salon.Auth.Login("administrator", replacement).Id);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.RecoverAccount("administrator", salon.RecoveryCode, TestSalon.Password));
    }

    [Fact]
    public void SessionIdentityCannotBeForgedOrElevatedByMutatingAccount()
    {
        using var salon = new TestSalon();
        var cashier = salon.Auth.CreateUser("cashier", TestSalon.Password, null, RoleKind.Cashier, 10, false, salon.Admin);
        var session = salon.Auth.Login("cashier", TestSalon.Password);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Require(new UserAccount { Id = session.Id, Role = RoleKind.Administrator }, true));
        session.Role = RoleKind.Administrator;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Require(session, true));
        session.Id = salon.Admin.Id;
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Require(session, true));
    }

    [Fact]
    public void ExpiredSessionsRequireAnewLogin()
    {
        using var salon = new TestSalon();
        salon.Auth.SessionTimeout = TimeSpan.FromTicks(-1);
        Assert.Throws<UnauthorizedAccessException>(() => salon.Auth.Require(salon.Admin));
        Assert.Null(salon.Auth.CurrentUser);
    }
}
