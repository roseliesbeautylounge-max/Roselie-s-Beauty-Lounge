using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roselie.Infrastructure;

namespace Roselie.App;

public partial class App
{
 private sealed class PersistenceExpectation
 {
  public int Version {get;set;}=1;
  public SortedDictionary<string,PersistentTable> Tables {get;set;}=new(StringComparer.Ordinal);
 }
 private sealed class PersistentTable
 {
  public long RowCount {get;set;}
  public string[] Columns {get;set;}=[];
  public string Sha256 {get;set;}="";
 }
 private static readonly HashSet<string> SecretPersistenceColumns=new(StringComparer.OrdinalIgnoreCase)
 {
  "PasswordHash","RecoveryCodeHash","PinHash","SuperPinHash","PinLookupHash"
 };

 // This verification only uses the smoke run's generated data folder, never the normal salon folder.
 internal static void WritePersistenceExpectation(DatabaseService database,string output)
 {
  EnsureIsolatedPersistenceFolder(database,output);
  WritePersistenceJson(Path.Combine(output,"persistence-expected.json"),CapturePersistence(database));
 }

 internal static bool VerifyPersistence(DatabaseService database,string output)
 {
  var differences=new List<string>();
  try
  {
   EnsureIsolatedPersistenceFolder(database,output);
   var path=Path.Combine(output,"persistence-expected.json");
   var expected=JsonSerializer.Deserialize<PersistenceExpectation>(File.ReadAllText(path))??throw new InvalidDataException("The persistence expectation is missing or empty.");
   if(expected.Version!=1||expected.Tables==null||expected.Tables.Count==0)throw new InvalidDataException("The persistence expectation format is unsupported or incomplete.");
   var actual=CapturePersistence(database);
   foreach(var table in expected.Tables.Keys.Union(actual.Tables.Keys).Order(StringComparer.Ordinal))
   {
    if(!expected.Tables.TryGetValue(table,out var before)||!actual.Tables.TryGetValue(table,out var after)){differences.Add(table+": table missing or unexpected");continue;}
    if(before.RowCount!=after.RowCount)differences.Add(table+": row count changed");
    if(before.Columns==null||!before.Columns.SequenceEqual(after.Columns,StringComparer.Ordinal))differences.Add(table+": persisted columns changed");
    if(!string.Equals(before.Sha256,after.Sha256,StringComparison.Ordinal))differences.Add(table+": saved values changed");
   }
   WritePersistenceJson(Path.Combine(output,"persistence-results.json"),new {Success=differences.Count==0,Integrity=true,ComparedTables=actual.Tables.Count,Differences=differences,Verification="Fresh process reopened the same isolated encrypted salon database; stable saved fields, receipt content and profile display names were compared."});
   return differences.Count==0;
  }
  catch(Exception ex)
  {
   WritePersistenceJson(Path.Combine(output,"persistence-results.json"),new {Success=false,Integrity=false,Error=ex.GetBaseException().Message,Differences=differences});
   return false;
  }
 }

 private static PersistenceExpectation CapturePersistence(DatabaseService database)
 {
  lock(database.Gate)
  {
   if(!database.CheckIntegrity())throw new InvalidDataException("The reopened salon database failed its integrity check.");
   using var context=database.OpenContext();var snapshot=new PersistenceExpectation();
   foreach(var table in context.Model.GetEntityTypes().Select(t=>t.GetTableName()).OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
   {
    var columns=new List<string>();
    using(var schema=context.Database.GetDbConnection().CreateCommand())
    {
     schema.CommandText="PRAGMA table_info("+PersistenceIdentifier(table)+");";
     using var reader=schema.ExecuteReader();while(reader.Read()){var column=reader.GetString(1);if(!SecretPersistenceColumns.Contains(column))columns.Add(column);}
    }
    if(columns.Count==0)throw new InvalidDataException("No persisted fields were found for "+table+".");
    using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(columns));hash.AppendData([10]);
    using var command=context.Database.GetDbConnection().CreateCommand();
    command.CommandText="SELECT "+string.Join(",",columns.Select(PersistenceIdentifier))+" FROM "+PersistenceIdentifier(table)+" ORDER BY "+PersistenceIdentifier("Id")+";";
    long count=0;var keyIndex=columns.FindIndex(c=>c=="Key");var valueIndex=columns.FindIndex(c=>c=="Value");
    using(var reader=command.ExecuteReader())
    {
     while(reader.Read())
     {
      var values=new object?[columns.Count];for(var index=0;index<values.Length;index++)values[index]=reader.IsDBNull(index)?null:reader.GetValue(index);
      if(table=="ApplicationSetting"&&keyIndex>=0&&valueIndex>=0&&values[keyIndex] is string key&&key.EndsWith("PassphraseProtected",StringComparison.OrdinalIgnoreCase))values[valueIndex]="<protected secret excluded>";
      hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(values));hash.AppendData([10]);count++;
     }
    }
    snapshot.Tables[table]=new PersistentTable {RowCount=count,Columns=columns.ToArray(),Sha256=Convert.ToHexString(hash.GetHashAndReset())};
   }
   return snapshot;
  }
 }
 private static string PersistenceIdentifier(string name)=>"\""+name.Replace("\"","\"\"")+"\"";
 private static void EnsureIsolatedPersistenceFolder(DatabaseService database,string output)
 {
  var isolated=Path.GetFullPath(Path.Combine(output,"isolated-data"));
  if(!string.Equals(Path.GetFullPath(database.DataDirectory),isolated,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Persistence verification is restricted to this run's isolated data folder.");
 }
 private static void WritePersistenceJson<T>(string path,T value)
 {
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
  var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try
  {
   using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
   {
    JsonSerializer.Serialize(stream,value,new JsonSerializerOptions {WriteIndented=true});stream.Flush(true);
   }
   File.Move(temporary,path,true);
  }
  finally{if(File.Exists(temporary))File.Delete(temporary);}
 }
}
