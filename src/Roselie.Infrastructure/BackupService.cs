using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;
namespace Roselie.Infrastructure;

public sealed class BackupService
{
    private readonly DatabaseService _database;
    private readonly AuthService _auth;
    public string? LastSafetyBackupPath { get; private set; }
    private sealed class Envelope
    {
        public string Format { get; set; } = "RoselieEncryptedBackupV1";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string Sha256 { get; set; } = "";
        public string Database { get; set; } = "";
        public bool Portable { get; set; }
        public bool RawKey { get; set; }
        public string WrappedKey { get; set; } = "";
        public string Salt { get; set; } = "";
        public string Nonce { get; set; } = "";
        public string Tag { get; set; } = "";
    }
    public BackupService(DatabaseService database,AuthService auth) { _database=database;_auth=auth; }
    public BackupRecord CreateBackup(string filePath,string passphrase,UserAccount user)
    {
        _auth.Require(user,true);if(passphrase.Length<12 || passphrase.Length>256) throw new ArgumentException("A portable backup requires a passphrase containing 12 to 256 characters.");return Create(filePath,passphrase,user.Id,false);
    }
    private BackupRecord Create(string filePath,string? passphrase,Guid? userId,bool scheduled)
    {
        filePath=Path.GetFullPath(filePath);if(_database.IsProtectedDataPath(filePath)) throw new ArgumentException("A backup cannot overwrite the salon database, encryption key, journal, or restore recovery files.");
        lock(_database.Gate)
        {
            if(!_database.CheckIntegrity()) throw new InvalidOperationException("The database failed its integrity check. No backup was created.");
            _database.Checkpoint();var key=_database.CopyKey();
            try
            {
                var bytes=File.ReadAllBytes(_database.DatabasePath);var envelope=new Envelope {Database=Convert.ToBase64String(bytes),Sha256=Convert.ToHexString(SHA256.HashData(bytes)),Portable=passphrase!=null,RawKey=_database.UseRawKey};
                if(passphrase==null) envelope.WrappedKey=Convert.ToBase64String(ProtectedData.Protect(key,null,DataProtectionScope.CurrentUser));
                else
                {
                    var salt=RandomNumberGenerator.GetBytes(32);var wrappingKey=Rfc2898DeriveBytes.Pbkdf2(passphrase,salt,600_000,HashAlgorithmName.SHA256,32);var nonce=RandomNumberGenerator.GetBytes(12);var cipher=new byte[key.Length];var tag=new byte[16];
                    try {using var aes=new AesGcm(wrappingKey,16);aes.Encrypt(nonce,key,cipher,tag,Encoding.UTF8.GetBytes(envelope.Format));} finally {CryptographicOperations.ZeroMemory(wrappingKey);}
                    envelope.WrappedKey=Convert.ToBase64String(cipher);envelope.Salt=Convert.ToBase64String(salt);envelope.Nonce=Convert.ToBase64String(nonce);envelope.Tag=Convert.ToBase64String(tag);
                }
                var parent=Path.GetDirectoryName(filePath)!;Directory.CreateDirectory(parent);var temporary=filePath+"."+Guid.NewGuid().ToString("N")+".tmp";
                try {DatabaseService.WriteFileDurably(temporary,JsonSerializer.SerializeToUtf8Bytes(envelope));File.Move(temporary,filePath,true);} finally {if(File.Exists(temporary)) File.Delete(temporary);}
                using var db=_database.OpenContext();var record=new BackupRecord {FilePath=filePath,Sha256=envelope.Sha256,SizeBytes=new FileInfo(filePath).Length,CreatedByUserId=userId,VerifiedAtUtc=DateTime.UtcNow,IsScheduled=scheduled};db.Add(record);AuthService.Audit(db,userId,scheduled?"Scheduled backup created":passphrase!=null?"Portable backup created":"Local encrypted backup created",record.Id,filePath,"BackupRecord");db.SaveChanges();return record;
            }
            finally {CryptographicOperations.ZeroMemory(key);}
        }
    }
    public bool VerifyBackup(string filePath,string passphrase,UserAccount user)
    {
        _auth.Require(user,true);var (bytes,key,rawKey)=Decode(filePath,passphrase);var stage=Path.Combine(_database.DataDirectory,"verify-"+Guid.NewGuid().ToString("N")+".db");
        try {File.WriteAllBytes(stage,bytes);ValidateDatabase(stage,key,rawKey);return true;} finally {CryptographicOperations.ZeroMemory(key);DeleteStage(stage);}
    }
    public void RestoreBackup(string filePath,string passphrase,UserAccount user)
    {
        _auth.Require(user,true);var (bytes,key,rawKey)=Decode(filePath,passphrase);
        lock(_database.Gate)
        {
            var stage=Path.Combine(_database.DataDirectory,"restore-"+Guid.NewGuid().ToString("N")+".db");var prepared=false;
            try
            {
                DatabaseService.WriteFileDurably(stage,bytes);ValidateDatabase(stage,key,rawKey);
                var safetyPath=Path.Combine(_database.DataDirectory,"Backups",$"BeforeRestore-{DateTime.UtcNow.AddHours(8):yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}.rblbackup");
                LastSafetyBackupPath=Create(safetyPath,null,user.Id,false).FilePath;
                _database.Checkpoint();_database.PrepareRestoreRecovery();prepared=true;
                DeleteSidecars(_database.DatabasePath);
                File.Replace(stage,_database.DatabasePath,null,true);_database.InstallKey(key,rawKey);
                _database.Initialize();
                using(var db=_database.OpenContext()) {AuthService.Audit(db,null,"Database restored",null,"Validated encrypted backup restored: "+Path.GetFileName(filePath),"BackupRecord");db.SaveChanges();}
                _database.CompleteRestoreRecovery();
                _auth.EndSessionAfterRestore();
            }
            catch(Exception restoreError)
            {
                if(prepared)
                {
                    try{_database.RollbackPendingRestore();}
                    catch(Exception recoveryError){throw new AggregateException("Restore was interrupted and automatic recovery could not finish. Preserve all files in "+_database.DataDirectory+" and the BeforeRestore backup.",restoreError,recoveryError);}
                }
                throw;
            }
            finally {CryptographicOperations.ZeroMemory(key);DeleteStage(stage);}
        }
    }
    public BackupRecord? RunScheduledIfDue()
    {
        lock(_database.Gate)
        {
            string directory;string protectedPassphrase;
            using(var db=_database.OpenContext())
            {
                string Setting(string key)=>db.Set<ApplicationSetting>().SingleOrDefault(s=>s.Key==key)?.Value??"";
                if(!int.TryParse(Setting("BackupScheduleHours"),out var hours) || hours<=0) return null;
                var last=db.Set<BackupRecord>().AsNoTracking().Where(b=>b.IsScheduled).OrderByDescending(b=>b.CreatedAtUtc).FirstOrDefault();if(last!=null && DateTime.UtcNow-last.CreatedAtUtc<TimeSpan.FromHours(hours)) return null;
                directory=Setting("BackupDirectory");if(string.IsNullOrWhiteSpace(directory)) directory=Path.Combine(_database.DataDirectory,"Backups");
                protectedPassphrase=Setting("BackupPassphraseProtected");
            }
            string? passphrase=null;
            if(!string.IsNullOrWhiteSpace(protectedPassphrase)) passphrase=Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedPassphrase),null,DataProtectionScope.CurrentUser));
            return Create(Path.Combine(directory,$"Roselie-{DateTime.UtcNow.AddHours(8):yyyyMMdd-HHmmss}.rblbackup"),passphrase,null,true);
        }
    }
    public static string ProtectScheduledPassphrase(string passphrase)
    {
        if(passphrase.Length<12) throw new ArgumentException("A backup passphrase needs at least 12 characters.");return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(passphrase),null,DataProtectionScope.CurrentUser));
    }
    private static (byte[] Database,byte[] Key,bool RawKey) Decode(string path,string passphrase)
    {
        Envelope envelope;
        try {envelope=JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path))??throw new InvalidDataException("Backup is empty.");} catch(JsonException ex) {throw new InvalidDataException("This is not a Roselie encrypted backup.",ex);}
        if(envelope.Format!="RoselieEncryptedBackupV1") throw new InvalidDataException("Unsupported backup format.");
        var bytes=Convert.FromBase64String(envelope.Database);var hash=SHA256.HashData(bytes);if(!CryptographicOperations.FixedTimeEquals(hash,Convert.FromHexString(envelope.Sha256))) throw new InvalidDataException("Backup content is damaged or incomplete.");
        if(!envelope.Portable) return(bytes,ProtectedData.Unprotect(Convert.FromBase64String(envelope.WrappedKey),null,DataProtectionScope.CurrentUser),envelope.RawKey);
        var salt=Convert.FromBase64String(envelope.Salt);var wrappingKey=Rfc2898DeriveBytes.Pbkdf2(passphrase,salt,600_000,HashAlgorithmName.SHA256,32);var key=new byte[32];
        try {using var aes=new AesGcm(wrappingKey,16);aes.Decrypt(Convert.FromBase64String(envelope.Nonce),Convert.FromBase64String(envelope.WrappedKey),Convert.FromBase64String(envelope.Tag),key,Encoding.UTF8.GetBytes(envelope.Format));return(bytes,key,envelope.RawKey);}
        catch(CryptographicException ex) {CryptographicOperations.ZeroMemory(key);throw new UnauthorizedAccessException("The backup passphrase is incorrect or the backup key is damaged.",ex);}
        finally {CryptographicOperations.ZeroMemory(wrappingKey);}
    }
    private static void ValidateDatabase(string path,byte[] key,bool rawKey)
    {
        using var db=DatabaseService.OpenContext(path,key,rawKey);using var command=db.Database.GetDbConnection().CreateCommand();command.CommandText="PRAGMA integrity_check;";if(command.ExecuteScalar()?.ToString()!="ok") throw new InvalidDataException("Backup database integrity validation failed.");command.CommandText="PRAGMA cipher_integrity_check;";using(var reader=command.ExecuteReader()) if(reader.Read()) throw new InvalidDataException("Backup encryption integrity validation failed.");command.CommandText="PRAGMA foreign_key_check;";using(var reader=command.ExecuteReader()) if(reader.Read()) throw new InvalidDataException("Backup database contains invalid relationships.");
        foreach(var table in db.Model.GetEntityTypes().Select(t=>t.GetTableName()).Distinct())
        {
            command.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";command.Parameters.Clear();var parameter=command.CreateParameter();parameter.ParameterName="$name";parameter.Value=table!;command.Parameters.Add(parameter);
            if(Convert.ToInt32(command.ExecuteScalar())!=1) throw new InvalidDataException($"Backup is missing required table {table}.");
        }
        command.Parameters.Clear();command.CommandText="SELECT COUNT(*) FROM UserAccount WHERE IsActive=1 AND Role=0;";if(Convert.ToInt32(command.ExecuteScalar())<1) throw new InvalidDataException("Backup contains no active administrator account.");
    }
    private static void DeleteSidecars(string path) {foreach(var suffix in new[]{"-wal","-shm"}) if(File.Exists(path+suffix)) File.Delete(path+suffix);}
    private static void DeleteStage(string path) {DeleteSidecars(path);if(File.Exists(path)) File.Delete(path);}
}


