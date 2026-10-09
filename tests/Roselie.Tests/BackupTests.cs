using System.Text.Json.Nodes;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Xunit;

namespace Roselie.Tests;

public sealed class BackupTests
{
    private const string Passphrase = "portable-backup-test-passphrase-2026!";

    [Fact]
    public void BackupRejectsWrongPassphraseWithoutChangingDatabase()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        var customer = business.Save(new Customer { FullName = "Backup customer" }, salon.Admin);
        var backup = new BackupService(salon.Database, salon.Auth);
        var path = Path.Combine(salon.DirectoryPath, "portable.rblbackup");
        backup.CreateBackup(path, Passphrase, salon.Admin);
        Assert.True(backup.VerifyBackup(path, Passphrase, salon.Admin));
        Assert.Throws<UnauthorizedAccessException>(() => backup.RestoreBackup(path, "wrong-passphrase", salon.Admin));
        Assert.Equal(customer.FullName, salon.Read<Customer>(customer.Id).FullName);
        Assert.True(salon.Database.CheckIntegrity());
    }

    [Fact]
    public void BackupDetectsTamperingBeforeDatabaseReplacement()
    {
        using var salon = new TestSalon();
        var backup = new BackupService(salon.Database, salon.Auth);
        var path = Path.Combine(salon.DirectoryPath, "tampered.rblbackup");
        backup.CreateBackup(path, Passphrase, salon.Admin);
        var envelope = JsonNode.Parse(File.ReadAllText(path))!;
        var bytes = Convert.FromBase64String(envelope["Database"]!.GetValue<string>());
        bytes[bytes.Length / 2] ^= 0x01;
        envelope["Database"] = Convert.ToBase64String(bytes);
        File.WriteAllText(path, envelope.ToJsonString());
        Assert.Throws<InvalidDataException>(() => backup.RestoreBackup(path, Passphrase, salon.Admin));
        Assert.Equal(40, salon.Count<SalonService>());
        Assert.True(salon.Database.CheckIntegrity());
        Assert.NotNull(salon.Auth.CurrentUser);
    }

    [Fact]
    public void PortableBackupRestoresOnAnIndependentInstallationWithItsOwnKey()
    {
        using var source = new TestSalon();
        var business = new BusinessService(source.Database, source.Auth);
        var customer = business.Save(new Customer { FullName = "Portable source customer" }, source.Admin);
        var path = Path.Combine(source.DirectoryPath, "portable.rblbackup");
        new BackupService(source.Database, source.Auth).CreateBackup(path, Passphrase, source.Admin);
        using var target = new TestSalon();
        Assert.NotEqual(source.Admin.Id, target.Admin.Id);
        new BackupService(target.Database, target.Auth).RestoreBackup(path, Passphrase, target.Admin);
        Assert.Null(target.Auth.CurrentUser);
        Assert.Equal(source.Admin.Id, target.Auth.Login("administrator", TestSalon.Password).Id);
        Assert.Equal(customer.FullName, target.Read<Customer>(customer.Id).FullName);
        Assert.True(target.Database.CheckIntegrity());
        var reopened = new DatabaseService(target.DirectoryPath);
        Assert.True(reopened.CheckIntegrity());
    }

    [Fact]
    public void ScheduledBackupRunsOncePerConfiguredInterval()
    {
        using var salon = new TestSalon();
        var business = new BusinessService(salon.Database, salon.Auth);
        business.SetSetting("BackupScheduleHours", "24", salon.Admin);
        business.SetSetting("BackupDirectory", Path.Combine(salon.DirectoryPath, "scheduled"), salon.Admin);
        business.SetSetting("BackupPassphraseProtected", BackupService.ProtectScheduledPassphrase(Passphrase), salon.Admin);
        var backups = new BackupService(salon.Database, salon.Auth);
        var first = backups.RunScheduledIfDue();
        Assert.NotNull(first);
        Assert.True(first.IsScheduled);
        Assert.True(backups.VerifyBackup(first.FilePath, Passphrase, salon.Admin));
        Assert.Null(backups.RunScheduledIfDue());
    }
}
