using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Storage;
using Roselie.Core.Models;

[assembly: InternalsVisibleTo("Roselie.Tests")]

namespace Roselie.Infrastructure;

public sealed class SalonDbContext : DbContext
{
    public SalonDbContext(DbContextOptions<SalonDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder model)
    {
        foreach (var type in typeof(Entity).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(Entity).IsAssignableFrom(t)))
        {
            var entity = model.Entity(type);
            entity.HasBaseType((Type?)null);
            entity.ToTable(type.Name);
            entity.HasKey(nameof(Entity.Id));
            foreach (var property in type.GetProperties())
            {
                if (property.PropertyType == typeof(decimal))
                {
                    var scale = IsQuantity(property.Name) ? 1_000_000m : 100m;
                    var converter = new ValueConverter<decimal, long>(v => checked((long)decimal.Round(v * scale, 0, MidpointRounding.AwayFromZero)), v => v / scale);
                    entity.Property(property.Name).HasConversion(converter).HasColumnType("INTEGER");
                }
                if (property.PropertyType == typeof(decimal?))
                {
                    var scale = IsQuantity(property.Name) ? 1_000_000m : 100m;
                    var converter = new ValueConverter<decimal?, long?>(v => v.HasValue ? checked((long)decimal.Round(v.Value * scale, 0, MidpointRounding.AwayFromZero)) : null, v => v.HasValue ? v.Value / scale : null);
                    entity.Property(property.Name).HasConversion(converter).HasColumnType("INTEGER");
                }
                if (property.PropertyType == typeof(string)) entity.Property(property.Name).HasMaxLength(4096);
            }
        }
        ConfigureConstraints(model);
    }
    private static bool IsQuantity(string name) => name.Contains("Quantity") || name.Contains("Percent") || name is "StockOnHand" or "ReorderLevel" or "BalanceAfter" or "AverageCost" or "UnitCost" or "PurchaseCost" or "CommissionRate" or "Rate";
    private static void ConfigureConstraints(ModelBuilder model)
    {
        var types = typeof(Entity).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(Entity).IsAssignableFrom(t)).ToDictionary(t => t.Name);
        void Unique(string typeName, params string[] properties)
        {
            if (types.TryGetValue(typeName, out var type) && properties.All(p => type.GetProperty(p) != null)) model.Entity(type).HasIndex(properties).IsUnique();
        }
        void Foreign(string child, string parent, string property)
        {
            if (types.TryGetValue(child, out var c) && types.TryGetValue(parent, out var p) && c.GetProperty(property) != null)
                model.Entity(c).HasOne(p, null).WithMany().HasForeignKey(property).OnDelete(DeleteBehavior.Restrict);
        }
        Unique("UserAccount", "NormalizedUsername"); Unique("Sale", "CheckoutToken"); Unique("Sale", "TransactionNumber"); Unique("ApplicationSetting", "Key");
        Foreign("SalonService", "ServiceCategory", "CategoryId"); Foreign("ServiceChargePreset", "SalonService", "ServiceId");
        Foreign("ServicePriceRule", "SalonService", "ServiceId"); Foreign("Product", "ProductCategory", "CategoryId"); Foreign("Product", "Supplier", "SupplierId");
        Foreign("UserAccount", "Employee", "EmployeeId"); Foreign("InventoryMovement", "Product", "ProductId");
        Foreign("ServiceMaterialRequirement", "SalonService", "ServiceId"); Foreign("ServiceMaterialRequirement", "Product", "ProductId");
        Foreign("ServiceMaterialConsumption", "Product", "ProductId"); Foreign("ServiceMaterialConsumption", "SaleItem", "SaleItemId");
        Foreign("Sale", "Customer", "CustomerId"); Foreign("Sale", "UserAccount", "CashierUserId"); Foreign("SaleItem", "Sale", "SaleId");
        Foreign("SaleItemCharge", "SaleItem", "SaleItemId"); Foreign("SaleItemDiscount", "SaleItem", "SaleItemId"); Foreign("Payment", "Sale", "SaleId");
        Foreign("Refund", "Sale", "SaleId"); Foreign("Refund", "UserAccount", "AuthorizedByUserId"); Foreign("Expense", "UserAccount", "RecordedByUserId");
        Foreign("Appointment", "Customer", "CustomerId"); Foreign("Appointment", "SalonService", "ServiceId"); Foreign("Appointment", "Employee", "EmployeeId");
        Foreign("PackageOffer", "SalonService", "ServiceId"); Foreign("CustomerPackage", "Customer", "CustomerId"); Foreign("CustomerPackage", "PackageOffer", "PackageOfferId"); Foreign("CustomerPackage", "SaleItem", "SaleItemId");
        Foreign("PackageRedemption", "CustomerPackage", "CustomerPackageId"); Foreign("PackageRedemption", "Employee", "EmployeeId");
        Foreign("CommissionRecord", "Employee", "EmployeeId"); Foreign("CommissionRecord", "SaleItem", "SaleItemId"); Foreign("ReceiptRecord", "Sale", "SaleId");
        Foreign("AuditLog", "UserAccount", "UserId");
        Unique("Role", "Kind"); Unique("Permission", "Key"); Unique("RolePermission", "RoleId", "PermissionId"); Unique("InventoryAverageCost", "ProductId"); Unique("EmployeeServiceCommission", "EmployeeId", "ServiceId"); Unique("ServiceMaterialRequirement", "ServiceId", "ProductId"); Unique("ReceiptRecord", "SaleId"); Unique("Refund", "SaleId");
        model.Entity<Expense>().HasIndex(e => e.CommissionRecordId).IsUnique().HasFilter("\"CommissionRecordId\" IS NOT NULL AND \"IsVoided\" = 0");
        Foreign("Expense", "UserAccount", "VoidedByUserId");
        if(typeof(UserAccount).GetProperty("PinLookupHash")!=null)model.Entity<UserAccount>().HasIndex("PinLookupHash").HasFilter("\"PinLookupHash\" IS NOT NULL");
        Foreign("UserAccount", "Role", "RoleId"); Foreign("RolePermission", "Role", "RoleId"); Foreign("RolePermission", "Permission", "PermissionId");
        Foreign("Employee", "UserAccount", "UserAccountId"); Foreign("EmployeeServiceCommission", "Employee", "EmployeeId"); Foreign("EmployeeServiceCommission", "SalonService", "ServiceId");
        Foreign("InventoryAverageCost", "Product", "ProductId"); Foreign("InventoryMovement", "SaleItem", "SaleItemId"); Foreign("InventoryMovement", "PackageRedemption", "PackageRedemptionId"); Foreign("InventoryMovement", "Refund", "RefundId"); Foreign("InventoryMovement", "UserAccount", "RecordedByUserId");
        Foreign("ServiceMaterialConsumption", "SalonService", "ServiceId"); Foreign("ServiceMaterialConsumption", "PackageRedemption", "PackageRedemptionId"); Foreign("ServiceMaterialConsumption", "UserAccount", "RecordedByUserId");
        Foreign("SaleItem", "Employee", "EmployeeId"); Foreign("SaleItem", "CustomerPackage", "CustomerPackageId"); Foreign("SaleItemDiscount", "UserAccount", "AuthorizedByUserId");
        Foreign("RefundItem", "Refund", "RefundId"); Foreign("RefundItem", "SaleItem", "SaleItemId"); Foreign("Expense", "CommissionRecord", "CommissionRecordId");
        Foreign("Appointment", "UserAccount", "RecordedByUserId"); Foreign("Appointment", "SaleItem", "SaleItemId"); Foreign("CustomerPackage", "SalonService", "ServiceId");
        Foreign("PackageRedemption", "SaleItem", "SaleItemId"); Foreign("PackageRedemption", "UserAccount", "RecordedByUserId"); Foreign("CommissionRecord", "PackageRedemption", "PackageRedemptionId");
        Foreign("ReceiptRecord", "UserAccount", "LastPrintedByUserId"); Foreign("BackupRecord", "UserAccount", "CreatedByUserId");
        model.Entity<Product>().HasIndex(p => p.Sku).IsUnique().HasFilter("\"Sku\" <> ''");
        model.Entity<Product>().HasIndex(p => p.Barcode).IsUnique().HasFilter("\"Barcode\" <> ''");
        model.Entity<CustomerPackage>().Ignore(p => p.RemainingSessions);
        model.Entity<Sale>().ToTable("Sale", t => t.HasCheckConstraint("CK_Sale_Amounts", "\"Total\" >= 0 AND \"DiscountAmount\" >= 0 AND \"Subtotal\" - \"DiscountAmount\" = \"Total\""));
        model.Entity<Product>().ToTable("Product", t => t.HasCheckConstraint("CK_Product_Stock", "\"StockQuantity\" >= 0 AND \"AverageCost\" >= 0 AND \"SellingPrice\" >= 0 AND \"PurchaseCost\" >= 0 AND \"ReorderLevel\" >= 0"));
        model.Entity<CustomerPackage>().ToTable("CustomerPackage", t => t.HasCheckConstraint("CK_Package_Sessions", "\"TotalSessions\" > 0 AND \"UsedSessions\" >= 0 AND \"UsedSessions\" <= \"TotalSessions\""));
        model.Entity<Payment>().ToTable("Payment", t => t.HasCheckConstraint("CK_Payment_Amount", "\"Amount\" > 0 AND \"AppliedAmount\" >= 0 AND \"AppliedAmount\" <= \"Amount\""));
        model.Entity<Expense>().ToTable("Expense", t => t.HasCheckConstraint("CK_Expense_Amount", "\"Amount\" >= 0"));
        model.Entity<SaleItem>().ToTable("SaleItem", t => t.HasCheckConstraint("CK_SaleItem_Amount", "\"Quantity\" > 0 AND \"Total\" >= 0 AND \"DiscountPercent\" BETWEEN 0 AND 100000000"));
    }
}

public sealed class DatabaseService
{
    private static int _initialized;
    private readonly string _keyFile;
    private byte[] _key = [];
    internal bool UseRawKey { get; private set; }
    public object Gate { get; } = new();
    public string DataDirectory { get; }
    public string DatabasePath { get; }
    public string? LastRecoveryDirectory { get; private set; }
    private string RestoreJournalPath => Path.Combine(DataDirectory, "restore-journal.json");
    public DatabaseService(string? dataDirectory = null)
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 0) SQLitePCL.Batteries_V2.Init();
        DataDirectory = Path.GetFullPath(dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RoselieBeautyLounge"));
        Directory.CreateDirectory(DataDirectory);
        DatabasePath = Path.Combine(DataDirectory, "salon.db");
        _keyFile = Path.Combine(DataDirectory, "database.key");
        RecoverPendingRestore();
        if (File.Exists(_keyFile))
        {
            (_key,UseRawKey)=ReadProtectedKey(_keyFile);
        }
        else
        {
            if (File.Exists(DatabasePath)) throw new InvalidOperationException("Database encryption key is missing. Restore an encrypted portable backup before continuing.");
            _key = RandomNumberGenerator.GetBytes(32);
            UseRawKey=true;
            try {WriteProtectedKey(_key,UseRawKey,false);}
            catch(IOException) when(File.Exists(_keyFile))
            {
                CryptographicOperations.ZeroMemory(_key);
                (_key,UseRawKey)=ReadProtectedKey(_keyFile);
            }
        }
    }
    public SalonDbContext OpenContext() => OpenContext(DatabasePath, _key,UseRawKey);
    internal static SalonDbContext OpenContext(string path, byte[] key,bool useRawKey=false,bool readOnly=false)
    {
        if(key.Length!=32) throw new InvalidDataException("A 256-bit database key is required.");
        var password=useRawKey?$"x'{Convert.ToHexString(key)}'":Convert.ToHexString(key);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Password = password, Pooling = false, DefaultTimeout = 30, ForeignKeys = true, Mode=readOnly?SqliteOpenMode.ReadOnly:SqliteOpenMode.ReadWriteCreate }.ToString());
        try
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA cipher_version;";
                if (command.ExecuteScalar() is not string cipher || string.IsNullOrWhiteSpace(cipher)) throw new InvalidOperationException("SQLCipher encryption provider is unavailable. The application refuses to open an unencrypted database.");
                command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=30000; PRAGMA synchronous=FULL;"; command.ExecuteNonQuery();
            }
            return new SalonDbContext(new DbContextOptionsBuilder<SalonDbContext>().UseSqlite(connection, true).Options);
        }
        catch{connection.Dispose();throw;}
    }
    public void Initialize()
    {
        lock (Gate)
        {
            using var db = OpenContext();
            db.Database.EnsureCreated();
            UpgradeSchema(db);
            db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            CatalogSeeder.Seed(db);
        }
    }
    private static void UpgradeSchema(SalonDbContext db)
    {
        using var transaction=db.Database.BeginTransaction();
        bool AddColumn(string table,string column,string definition)
        {
            using var command=db.Database.GetDbConnection().CreateCommand();command.Transaction=transaction.GetDbTransaction();command.CommandText=$"PRAGMA table_info(\"{table}\");";
            using(var reader=command.ExecuteReader()) {while(reader.Read()) if(reader.GetString(1)==column) return false;}
            // Identifiers and definitions are fixed literals supplied below; no user input reaches this SQL.
            command.CommandText=$"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};";command.ExecuteNonQuery();return true;
        }
        var productValueAdded=AddColumn("Product","InventoryValue","INTEGER NOT NULL DEFAULT 0");
        var averageValueAdded=AddColumn("InventoryAverageCost","InventoryValue","INTEGER NOT NULL DEFAULT 0");
        var movementValueAdded=AddColumn("InventoryMovement","ValueChange","INTEGER NOT NULL DEFAULT 0");
        AddColumn("SaleItem","PriceRuleId","TEXT NULL");
        AddColumn("UserAccount","PinHash","TEXT NULL");
        AddColumn("UserAccount","DisplayName","TEXT NOT NULL DEFAULT ''");
        AddColumn("UserAccount","SuperPinHash","TEXT NULL");
        AddColumn("UserAccount","SuperPinFailedAttempts","INTEGER NOT NULL DEFAULT 0");
        AddColumn("UserAccount","SuperPinLockedUntilUtc","TEXT NULL");
        AddColumn("UserAccount","PinLookupHash","TEXT NULL");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_UserAccount_PinLookupHash\" ON \"UserAccount\"(\"PinLookupHash\") WHERE \"PinLookupHash\" IS NOT NULL;");
        AddColumn("SalonService","IsDeleted","INTEGER NOT NULL DEFAULT 0");
        AddColumn("PackageOffer","IsDeleted","INTEGER NOT NULL DEFAULT 0");
        AddColumn("Expense","IsVoided","INTEGER NOT NULL DEFAULT 0");
        AddColumn("Expense","VoidedAtUtc","TEXT NULL");
        AddColumn("Expense","VoidedByUserId","TEXT NULL REFERENCES \"UserAccount\"(\"Id\")");
        AddColumn("Expense","VoidReason","TEXT NOT NULL DEFAULT ''");
        using(var indexCommand=db.Database.GetDbConnection().CreateCommand())
        {
            indexCommand.Transaction=transaction.GetDbTransaction();
            indexCommand.CommandText="SELECT sql FROM sqlite_master WHERE type='index' AND name='IX_Expense_CommissionRecordId';";
            if(indexCommand.ExecuteScalar() is not string definition||!definition.Contains("IsVoided",StringComparison.OrdinalIgnoreCase))
            {
                indexCommand.CommandText="DROP INDEX IF EXISTS \"IX_Expense_CommissionRecordId\"; CREATE UNIQUE INDEX \"IX_Expense_CommissionRecordId\" ON \"Expense\"(\"CommissionRecordId\") WHERE \"CommissionRecordId\" IS NOT NULL AND \"IsVoided\"=0;";
                indexCommand.ExecuteNonQuery();
            }
        }
        if(productValueAdded) foreach(var product in db.Set<Product>()) product.InventoryValue=Roselie.Core.Finance.FinancialEngine.RoundMoney(product.StockQuantity*product.AverageCost);
        if(averageValueAdded)
        {
            var products=db.Set<Product>().ToDictionary(p=>p.Id);
            foreach(var average in db.Set<InventoryAverageCost>()) if(products.TryGetValue(average.ProductId,out var product)) average.InventoryValue=product.InventoryValue;
        }
        if(movementValueAdded) foreach(var movement in db.Set<InventoryMovement>()) movement.ValueChange=Roselie.Core.Finance.FinancialEngine.RoundMoney(movement.Quantity*movement.UnitCost);
        db.SaveChanges();transaction.Commit();
    }
    public bool CheckIntegrity()
    {
        lock (Gate)
        {
            using var db = OpenContext();
            using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            if (!string.Equals(cmd.ExecuteScalar()?.ToString(), "ok", StringComparison.Ordinal)) return false;
            cmd.CommandText = "PRAGMA cipher_integrity_check;"; using (var reader = cmd.ExecuteReader()) if (reader.Read()) return false;
            cmd.CommandText = "PRAGMA foreign_key_check;"; using (var reader = cmd.ExecuteReader()) if (reader.Read()) return false;
            return true;
        }
    }
    internal byte[] CopyKey() => _key.ToArray();
    internal void InstallKey(byte[] key,bool useRawKey=false)
    {
        WriteProtectedKey(key,useRawKey);
        CryptographicOperations.ZeroMemory(_key); _key = key.ToArray();UseRawKey=useRawKey;
    }
    private void WriteProtectedKey(byte[] key,bool useRawKey,bool overwrite=true)
    {
        var payload=useRawKey?new byte[33]:key.ToArray();if(useRawKey) {payload[0]=1;key.CopyTo(payload,1);}
        var temporary=_keyFile+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {WriteFileDurably(temporary,ProtectedData.Protect(payload,null,DataProtectionScope.CurrentUser));File.Move(temporary,_keyFile,overwrite);} finally {CryptographicOperations.ZeroMemory(payload);if(File.Exists(temporary)) File.Delete(temporary);}
    }
    internal void Checkpoint(int busyTimeoutMilliseconds=30000)
    {
        if(busyTimeoutMilliseconds<0||busyTimeoutMilliseconds>30000)throw new ArgumentOutOfRangeException(nameof(busyTimeoutMilliseconds));
        using var db = OpenContext();
        using var command=db.Database.GetDbConnection().CreateCommand();
        command.CommandText="PRAGMA busy_timeout="+busyTimeoutMilliseconds+";";command.ExecuteNonQuery();
        command.CommandText="PRAGMA wal_checkpoint(TRUNCATE);";
        using var reader=command.ExecuteReader();
        if(!reader.Read() || reader.GetInt32(0)!=0 || reader.GetInt32(1)!=reader.GetInt32(2))
            throw new IOException("The database is busy and could not finish its checkpoint. No database files were copied or replaced. Close other database readers and retry.");
    }

    internal bool IsProtectedDataPath(string path)
    {
        path=Path.GetFullPath(path);
        return new[]{DatabasePath,_keyFile,Path.Combine(DataDirectory,"pin-lookup.key"),DatabasePath+"-wal",DatabasePath+"-shm",DatabasePath+"-journal",RestoreJournalPath}.Any(p=>string.Equals(path,p,StringComparison.OrdinalIgnoreCase))
            || path.StartsWith(Path.Combine(DataDirectory,"restore-recovery-"),StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RestoreJournal
    {
        [JsonRequired] public int Version {get;set;}=1;
        [JsonRequired] public string Id {get;set;}="";
        [JsonRequired] public string Status {get;set;}="Pending";
        [JsonRequired] public string DatabaseSha256 {get;set;}="";
        [JsonRequired] public string KeySha256 {get;set;}="";
        [JsonRequired] public long DatabaseLength {get;set;}
        [JsonRequired] public long KeyLength {get;set;}
        [JsonRequired] public string CommittedKeySha256 {get;set;}="";
    }

    // An encrypted database and its DPAPI key are a pair. Preserve both before replacing either.
    internal void PrepareRestoreRecovery()
    {
        if(File.Exists(RestoreJournalPath)) throw new IOException("A previous restore requires recovery. Restart Roselie's POS before restoring another backup.");
        var journal=new RestoreJournal {Id=Guid.NewGuid().ToString("N")};
        var directory=RecoveryDirectory(journal);Directory.CreateDirectory(directory);
        var previousDatabase=Path.Combine(directory,"previous.db");var previousKey=Path.Combine(directory,"previous.key");
        CopyFileDurably(DatabasePath,previousDatabase);CopyFileDurably(_keyFile,previousKey);
        journal.DatabaseSha256=FileHash(previousDatabase);journal.KeySha256=FileHash(previousKey);
        journal.DatabaseLength=new FileInfo(previousDatabase).Length;journal.KeyLength=new FileInfo(previousKey).Length;
        ValidateRecoveryPair(previousDatabase,previousKey);
        WriteRestoreJournal(journal);
    }

    internal void CompleteRestoreRecovery()
    {
        var journal=ReadRestoreJournal();
        Checkpoint();ValidateRecoveryPair(DatabasePath,_keyFile);
        journal.Status="Committed";journal.CommittedKeySha256=FileHash(_keyFile);
        WriteRestoreJournal(journal);
        CleanupCompletedRestore(journal);
    }

    internal void RollbackPendingRestore()
    {
        RecoverPendingRestore();
        CryptographicOperations.ZeroMemory(_key);(_key,UseRawKey)=ReadProtectedKey(_keyFile);
    }

    private void RecoverPendingRestore()
    {
        if(!File.Exists(RestoreJournalPath))return;
        var journal=ReadRestoreJournal();var directory=RecoveryDirectory(journal);
        if(journal.Status=="Committed")
        {
            if(!File.Exists(_keyFile)||FileHash(_keyFile)!=journal.CommittedKeySha256)throw RecoveryError(directory);
            ValidateRecoveryPair(DatabasePath,_keyFile);CleanupCompletedRestore(journal);return;
        }
        var previousDatabase=Path.Combine(directory,"previous.db");var previousKey=Path.Combine(directory,"previous.key");
        if(!File.Exists(previousDatabase)||!File.Exists(previousKey)
            ||new FileInfo(previousDatabase).Length!=journal.DatabaseLength||new FileInfo(previousKey).Length!=journal.KeyLength
            ||FileHash(previousDatabase)!=journal.DatabaseSha256||FileHash(previousKey)!=journal.KeySha256)throw RecoveryError(directory);
        ValidateRecoveryPair(previousDatabase,previousKey);
        if(journal.Status=="RolledBack")
        {
            if(!File.Exists(_keyFile)||FileHash(_keyFile)!=journal.KeySha256)throw RecoveryError(directory);
            ValidateRecoveryPair(DatabasePath,_keyFile);LastRecoveryDirectory=directory;TryDeleteRestoreJournal();return;
        }
        // Atomic preservation copies survive a second interruption during recovery. Never consume the old pair.
        foreach(var (source,name) in new[]{(DatabasePath,"interrupted.db"),(_keyFile,"interrupted.key"),(DatabasePath+"-wal","interrupted.db-wal"),(DatabasePath+"-shm","interrupted.db-shm"),(DatabasePath+"-journal","interrupted.db-journal")})
            if(File.Exists(source)&&!File.Exists(Path.Combine(directory,name)))CopyFileDurably(source,Path.Combine(directory,name));
        foreach(var suffix in new[]{"-wal","-shm","-journal"})if(File.Exists(DatabasePath+suffix))File.Delete(DatabasePath+suffix);
        CopyFileDurably(previousDatabase,DatabasePath,true);CopyFileDurably(previousKey,_keyFile,true);
        ValidateRecoveryPair(DatabasePath,_keyFile);
        if(_key.Length>0){CryptographicOperations.ZeroMemory(_key);(_key,UseRawKey)=ReadProtectedKey(_keyFile);}
        journal.Status="RolledBack";WriteRestoreJournal(journal);
        LastRecoveryDirectory=directory;TryDeleteRestoreJournal();
    }

    private RestoreJournal ReadRestoreJournal()
    {
        RestoreJournal? journal;
        try{journal=JsonSerializer.Deserialize<RestoreJournal>(File.ReadAllText(RestoreJournalPath));}
        catch(JsonException ex){throw new InvalidDataException("Restore recovery journal is damaged. All salon and recovery files have been preserved in "+DataDirectory+".",ex);}
        if(journal==null||journal.Version!=1||!Guid.TryParseExact(journal.Id,"N",out _)||journal.Status is not ("Pending" or "Committed" or "RolledBack")
            ||journal.DatabaseLength<=0||journal.KeyLength<=0||!ValidHash(journal.DatabaseSha256)||!ValidHash(journal.KeySha256)
            ||journal.Status=="Committed"&&!ValidHash(journal.CommittedKeySha256))throw RecoveryError(DataDirectory);
        return journal;
    }
    private static bool ValidHash(string? hash)=>hash is {Length:64}&&hash.All(Uri.IsHexDigit);
    private string RecoveryDirectory(RestoreJournal journal)=>Path.Combine(DataDirectory,"restore-recovery-"+journal.Id);
    private static InvalidDataException RecoveryError(string directory)=>new("Restore recovery could not verify its database/key pair. No recovery files were removed. Preserve the files in "+directory+" and restore a verified encrypted backup.");
    private void WriteRestoreJournal(RestoreJournal journal)
    {
        var temporary=RestoreJournalPath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{WriteFileDurably(temporary,JsonSerializer.SerializeToUtf8Bytes(journal));File.Move(temporary,RestoreJournalPath,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    private void CleanupCompletedRestore(RestoreJournal journal)
    {
        try
        {
            if(!TryDeleteRestoreJournal())return;
            var directory=RecoveryDirectory(journal);
            foreach(var name in new[]{"previous.db","previous.db-wal","previous.db-shm","previous.key"})
                if(File.Exists(Path.Combine(directory,name)))File.Delete(Path.Combine(directory,name));
            if(Directory.Exists(directory)&&!Directory.EnumerateFileSystemEntries(directory).Any())Directory.Delete(directory);
        }
        catch(IOException){}catch(UnauthorizedAccessException){}
    }
    private bool TryDeleteRestoreJournal()
    {
        try{File.Delete(RestoreJournalPath);return true;}
        catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}
    }
    private static (byte[] Key,bool RawKey) ReadProtectedKey(string path)
    {
        var payload=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser);
        if(payload.Length==32)return(payload,false);
        try{if(payload.Length==33&&payload[0]==1)return(payload[1..],true);throw new InvalidDataException("Database key format is unsupported.");}
        finally{CryptographicOperations.ZeroMemory(payload);}
    }
    private static void ValidateRecoveryPair(string database,string keyFile)
    {
        if(!File.Exists(database)||new FileInfo(database).Length<1024||!File.Exists(keyFile))throw RecoveryError(Path.GetDirectoryName(database)!);
        var (key,raw)=ReadProtectedKey(keyFile);
        try
        {
            using var db=OpenContext(database,key,raw,true);using var command=db.Database.GetDbConnection().CreateCommand();
            command.CommandText="PRAGMA integrity_check;";if(command.ExecuteScalar()?.ToString()!="ok")throw RecoveryError(Path.GetDirectoryName(database)!);
            command.CommandText="PRAGMA cipher_integrity_check;";using(var reader=command.ExecuteReader())if(reader.Read())throw RecoveryError(Path.GetDirectoryName(database)!);
            command.CommandText="PRAGMA foreign_key_check;";using(var reader=command.ExecuteReader())if(reader.Read())throw RecoveryError(Path.GetDirectoryName(database)!);
            command.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('UserAccount','Sale','ApplicationSetting');";
            if(Convert.ToInt32(command.ExecuteScalar())!=3)throw RecoveryError(Path.GetDirectoryName(database)!);
            command.CommandText="SELECT COUNT(*) FROM UserAccount WHERE IsActive=1 AND Role=0;";
            if(Convert.ToInt32(command.ExecuteScalar())<1)throw RecoveryError(Path.GetDirectoryName(database)!);
        }
        finally{CryptographicOperations.ZeroMemory(key);}
    }
    private static string FileHash(string path){using var file=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(file));}
    internal static void WriteFileDurably(string path,ReadOnlySpan<byte> content)
    {
        using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
        stream.Write(content);stream.Flush(true);
    }
    internal static void CopyFileDurably(string source,string destination,bool overwrite=false)
    {
        var temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var input=File.OpenRead(source))using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,FileOptions.WriteThrough)){input.CopyTo(output);output.Flush(true);}
            File.Move(temporary,destination,overwrite);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}




