using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class DatabaseDurabilityTests
{
    private const string Passphrase="durability-backup-test-passphrase-2026!";
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static string JournalPath(TestSalon salon)=>Path.Combine(salon.DirectoryPath,"restore-journal.json");
    private static string RecoveryDirectory(TestSalon salon)=>Path.Combine(salon.DirectoryPath,"restore-recovery-"+JsonNode.Parse(File.ReadAllText(JournalPath(salon)))!["Id"]!.GetValue<string>());

    [Fact]
    public void EveryNewConnectionUsesFullSynchronizationAndPersistentWal()
    {
        using var salon=new TestSalon();
        var reopened=new DatabaseService(salon.DirectoryPath);reopened.Initialize();
        foreach(var database in new[]{salon.Database,reopened})
        {
            using var context=database.OpenContext();using var command=context.Database.GetDbConnection().CreateCommand();
            command.CommandText="PRAGMA synchronous;";Assert.Equal(2L,Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText="PRAGMA journal_mode;";Assert.Equal("wal",command.ExecuteScalar()?.ToString());
        }
    }

    [Fact]
    public async Task CompetingFirstOpenersLoadOneKeyWithoutReplacingIt()
    {
        using var salon=new TestSalon();
        var directory=Path.Combine(salon.DirectoryPath,"competing-first-opens");
        using var start=new ManualResetEventSlim();
        var tasks=Enumerable.Range(0,8).Select(_=>Task.Run(()=>{start.Wait();return new DatabaseService(directory);})).ToArray();
        start.Set();var databases=await Task.WhenAll(tasks);
        var keyPath=Path.Combine(directory,"database.key");var keyBytes=File.ReadAllBytes(keyPath);
        databases[0].Initialize();
        foreach(var database in databases){Assert.True(database.CheckIntegrity());Assert.Equal(keyBytes,File.ReadAllBytes(keyPath));}
        new DatabaseService(directory).Initialize();Assert.Equal(keyBytes,File.ReadAllBytes(keyPath));
    }

    [Theory]
    [InlineData("salon.db")]
    [InlineData("database.key")]
    [InlineData("pin-lookup.key")]
    [InlineData("salon.db-wal")]
    [InlineData("salon.db-shm")]
    [InlineData("salon.db-journal")]
    [InlineData("restore-journal.json")]
    [InlineData("restore-recovery-protected/previous.key")]
    public void BackupRejectsEveryActiveDatabaseAndRecoveryTarget(string relativePath)
    {
        using var salon=new TestSalon();var backups=new BackupService(salon.Database,salon.Auth);
        var key=File.ReadAllBytes(Path.Combine(salon.DirectoryPath,"database.key"));
        Assert.Throws<ArgumentException>(()=>backups.CreateBackup(Path.Combine(salon.DirectoryPath,relativePath),Passphrase,salon.Admin));
        Assert.Equal(key,File.ReadAllBytes(Path.Combine(salon.DirectoryPath,"database.key")));
        Assert.True(salon.Database.CheckIntegrity());Assert.Equal(salon.Admin.Id,new AuthService(new DatabaseService(salon.DirectoryPath)).Login("administrator",TestSalon.Password).Id);
    }

    [Fact]
    public void CheckpointRefusesPinnedWalFramesAndBackupIncludesThemAfterReaderCloses()
    {
        using var salon=new TestSalon();var business=new BusinessService(salon.Database,salon.Auth);
        using(var reader=salon.Database.OpenContext())
        using(var transaction=((SqliteConnection)reader.Database.GetDbConnection()).BeginTransaction(deferred:true))
        {
            using(var snapshot=reader.Database.GetDbConnection().CreateCommand())
            {
                snapshot.Transaction=transaction;snapshot.CommandText="SELECT COUNT(*) FROM Customer;";
                Assert.Equal(0L,Convert.ToInt64(snapshot.ExecuteScalar()));
            }
            business.Save(new Customer {FullName="Committed while another reader pinned WAL"},salon.Admin);
            Assert.Throws<IOException>(()=>salon.Database.Checkpoint(0));
        }
        var backupPath=Path.Combine(salon.DirectoryPath,"complete.rblbackup");
        new BackupService(salon.Database,salon.Auth).CreateBackup(backupPath,Passphrase,salon.Admin);
        using var target=new TestSalon();new BackupService(target.Database,target.Auth).RestoreBackup(backupPath,Passphrase,target.Admin);
        Assert.Equal("Committed while another reader pinned WAL",Assert.Single(new BusinessService(target.Database,target.Auth).List<Customer>(target.Auth.Login("administrator",TestSalon.Password))).FullName);
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void StartupRecoversInterruptedCrossKeyRestoreAndPreservesBothPairs(bool keyInstalled,bool halfRollback)
    {
        using var source=new TestSalon();using var target=new TestSalon();
        var sourceCustomer=new BusinessService(source.Database,source.Auth).Save(new Customer {FullName="Incoming restored customer"},source.Admin);
        var targetCustomer=new BusinessService(target.Database,target.Auth).Save(new Customer {FullName="Original committed customer"},target.Admin);
        source.Database.Checkpoint();target.Database.Checkpoint();target.Database.PrepareRestoreRecovery();
        var recovery=RecoveryDirectory(target);var previousHash=Hash(Path.Combine(recovery,"previous.db"));
        InstallDatabaseCopy(source,target,keyInstalled);
        if(halfRollback)
        {
            DatabaseService.CopyFileDurably(target.Database.DatabasePath,Path.Combine(recovery,"interrupted.db"));
            DatabaseService.CopyFileDurably(Path.Combine(target.DirectoryPath,"database.key"),Path.Combine(recovery,"interrupted.key"));
            DatabaseService.CopyFileDurably(Path.Combine(recovery,"previous.db"),target.Database.DatabasePath,true);
        }
        // A new key loader represents a new application process after the interruption.
        var reopened=new DatabaseService(target.DirectoryPath);reopened.Initialize();
        Assert.True(reopened.CheckIntegrity());Assert.Equal(recovery,reopened.LastRecoveryDirectory);
        var auth=new AuthService(reopened);var user=auth.Login("administrator",TestSalon.Password);
        Assert.Equal(target.Admin.Id,user.Id);
        var customer=Assert.Single(new BusinessService(reopened,auth).List<Customer>(user));Assert.Equal(targetCustomer.Id,customer.Id);
        Assert.NotEqual(sourceCustomer.Id,customer.Id);Assert.False(File.Exists(JournalPath(target)));
        Assert.Equal(previousHash,Hash(Path.Combine(recovery,"previous.db")));
        Assert.Equal(Hash(source.Database.DatabasePath),Hash(Path.Combine(recovery,"interrupted.db")));
        Assert.True(File.Exists(Path.Combine(recovery,"previous.key")));Assert.True(File.Exists(Path.Combine(recovery,"interrupted.key")));
    }

    [Fact]
    public void RuntimeRollbackReloadsTheOriginalKeyForTheExistingService()
    {
        using var source=new TestSalon();using var target=new TestSalon();
        var original=new BusinessService(target.Database,target.Auth).Save(new Customer {FullName="Original runtime customer"},target.Admin);
        source.Database.Checkpoint();target.Database.Checkpoint();target.Database.PrepareRestoreRecovery();InstallDatabaseCopy(source,target,true);
        target.Database.RollbackPendingRestore();
        var business=new BusinessService(target.Database,target.Auth);
        Assert.Equal(original.Id,Assert.Single(business.List<Customer>(target.Admin)).Id);
        business.Save(new Customer {FullName="Saved after failed restore rollback"},target.Admin);
        Assert.Equal(2,business.List<Customer>(target.Admin).Count);Assert.True(target.Database.CheckIntegrity());
    }

    [Fact]
    public void DeferredRolledBackJournalCleanupStillReloadsTheExistingServiceKey()
    {
        using var source=new TestSalon();using var target=new TestSalon();
        source.Database.Checkpoint();target.Database.Checkpoint();target.Database.PrepareRestoreRecovery();InstallDatabaseCopy(source,target,true);
        var recovery=RecoveryDirectory(target);
        DatabaseService.CopyFileDurably(Path.Combine(recovery,"previous.db"),target.Database.DatabasePath,true);
        DatabaseService.CopyFileDurably(Path.Combine(recovery,"previous.key"),Path.Combine(target.DirectoryPath,"database.key"),true);
        var journal=JsonNode.Parse(File.ReadAllText(JournalPath(target)))!;journal["Status"]="RolledBack";
        File.WriteAllText(JournalPath(target),journal.ToJsonString());
        using(var heldJournal=new FileStream(JournalPath(target),FileMode.Open,FileAccess.Read,FileShare.Read))
        {
            target.Database.RollbackPendingRestore();
            Assert.True(File.Exists(JournalPath(target)));
            new BusinessService(target.Database,target.Auth).Save(new Customer {FullName="Saved with deferred journal cleanup"},target.Admin);
            Assert.True(target.Database.CheckIntegrity());
        }
        var reopened=new DatabaseService(target.DirectoryPath);Assert.True(reopened.CheckIntegrity());
        Assert.False(File.Exists(JournalPath(target)));Assert.True(File.Exists(Path.Combine(recovery,"previous.key")));
    }

    [Theory]
    [InlineData("missing-field")]
    [InlineData("null-hash")]
    [InlineData("damaged-key")]
    [InlineData("missing-database")]
    public void AmbiguousRecoveryPreservesActiveDatabaseKeyAndJournal(string damage)
    {
        using var salon=new TestSalon();salon.Database.Checkpoint();salon.Database.PrepareRestoreRecovery();
        var recovery=RecoveryDirectory(salon);var journal=JsonNode.Parse(File.ReadAllText(JournalPath(salon)))!;
        if(damage=="missing-field")journal.AsObject().Remove("Status");
        else if(damage=="null-hash")journal["KeySha256"]=null;
        else if(damage=="damaged-key")File.AppendAllText(Path.Combine(recovery,"previous.key"),"damaged");
        else File.Delete(Path.Combine(recovery,"previous.db"));
        File.WriteAllText(JournalPath(salon),journal.ToJsonString());
        var databaseHash=Hash(salon.Database.DatabasePath);var keyHash=Hash(Path.Combine(salon.DirectoryPath,"database.key"));var journalHash=Hash(JournalPath(salon));
        Assert.Throws<InvalidDataException>(()=>new DatabaseService(salon.DirectoryPath));
        Assert.Equal(databaseHash,Hash(salon.Database.DatabasePath));Assert.Equal(keyHash,Hash(Path.Combine(salon.DirectoryPath,"database.key")));
        Assert.Equal(journalHash,Hash(JournalPath(salon)));Assert.True(Directory.Exists(recovery));
    }

    [Fact]
    public void CommittedJournalKeepsNewDatabaseAndLaterWritesEvenWithPartiallyCleanedRecoveryFiles()
    {
        using var source=new TestSalon();using var target=new TestSalon();
        new BusinessService(source.Database,source.Auth).Save(new Customer {FullName="Restored committed customer"},source.Admin);
        source.Database.Checkpoint();target.Database.Checkpoint();target.Database.PrepareRestoreRecovery();InstallDatabaseCopy(source,target,true);
        var auth=new AuthService(target.Database);var user=auth.Login("administrator",TestSalon.Password);
        new BusinessService(target.Database,auth).Save(new Customer {FullName="Saved after restore completion"},user);
        target.Database.Checkpoint();var recovery=RecoveryDirectory(target);MarkCommitted(target);
        File.Delete(Path.Combine(recovery,"previous.key"));
        var reopened=new DatabaseService(target.DirectoryPath);reopened.Initialize();
        var reopenedAuth=new AuthService(reopened);var reopenedUser=reopenedAuth.Login("administrator",TestSalon.Password);
        Assert.Equal(source.Admin.Id,reopenedUser.Id);
        Assert.Equal(2,new BusinessService(reopened,reopenedAuth).List<Customer>(reopenedUser).Count);
        Assert.False(File.Exists(JournalPath(target)));Assert.True(reopened.CheckIntegrity());
    }

    [Fact]
    public void CommittedJournalRejectsEmptyDatabaseAndKeepsRecoveryPair()
    {
        using var salon=new TestSalon();salon.Database.Checkpoint();salon.Database.PrepareRestoreRecovery();
        var recovery=RecoveryDirectory(salon);MarkCommitted(salon);File.WriteAllBytes(salon.Database.DatabasePath,[]);
        Assert.Throws<InvalidDataException>(()=>new DatabaseService(salon.DirectoryPath));
        Assert.True(File.Exists(JournalPath(salon)));Assert.True(File.Exists(Path.Combine(recovery,"previous.db")));Assert.True(File.Exists(Path.Combine(recovery,"previous.key")));
        Assert.Equal(0,new FileInfo(salon.Database.DatabasePath).Length);
    }

    private static void InstallDatabaseCopy(TestSalon source,TestSalon target,bool installKey)
    {
        var replacement=Path.Combine(target.DirectoryPath,"interruption-test.db");DatabaseService.CopyFileDurably(source.Database.DatabasePath,replacement);
        File.Replace(replacement,target.Database.DatabasePath,null,true);
        if(installKey){var key=source.Database.CopyKey();try{target.Database.InstallKey(key,source.Database.UseRawKey);}finally{CryptographicOperations.ZeroMemory(key);}}
    }
    private static void MarkCommitted(TestSalon salon)
    {
        var journal=JsonNode.Parse(File.ReadAllText(JournalPath(salon)))!;
        journal["Status"]="Committed";journal["CommittedKeySha256"]=Hash(Path.Combine(salon.DirectoryPath,"database.key"));
        File.WriteAllText(JournalPath(salon),journal.ToJsonString());
    }
}
