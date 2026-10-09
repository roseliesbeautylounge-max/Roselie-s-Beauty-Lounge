using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Roselie.Setup;

public partial class App : Application
{
 private const string ProductId="RoselieBeautyLoungePOS";
 private const string UninstallKey="Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\"+ProductId;
 private sealed record Manifest(string Product,Dictionary<string,string> Files);
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  if(e.Args.Length==2&&e.Args[0]=="--verify-payload")
  {
   try{var directory=Path.GetFullPath(e.Args[1]);Directory.CreateDirectory(directory);ExtractAndVerify(directory);File.WriteAllText(Path.Combine(directory,"payload-verified.txt"),"All bundled application files passed SHA-256 verification.");Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetFullPath(e.Args[1]),"payload-error.txt"),ex.ToString());Shutdown(2);}return;
  }
  if(e.Args.Length==1&&e.Args[0]=="--uninstall"){Uninstall();return;}
  var uiSmoke=e.Args.Length==2&&e.Args[0]=="--ui-smoke";
  var defaultPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Roselie POS");
  try{using var registered=Registry.CurrentUser.OpenSubKey(UninstallKey);if(registered?.GetValue("InstallLocation") is string existing&&File.Exists(Path.Combine(existing,"install-manifest.json")))defaultPath=existing;}
  catch(Exception ex) when(ex is System.Security.SecurityException or UnauthorizedAccessException or IOException or ArgumentException){/* Optional update-path discovery must not prevent installer startup. */}
  var rose=new SolidColorBrush(Color.FromRgb(168,94,103));var ink=new SolidColorBrush(Color.FromRgb(52,64,82));var line=new SolidColorBrush(Color.FromRgb(233,218,219));
  var window=new Window{Title="Install Roselie's Beauty Lounge",Width=640,Height=610,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=uiSmoke?WindowStartupLocation.Manual:WindowStartupLocation.CenterScreen,Background=new SolidColorBrush(Color.FromRgb(250,246,245)),Foreground=ink,FontFamily=new FontFamily("Segoe UI"),Icon=BitmapFrame.Create(new Uri("pack://application:,,,/Roselie.Setup;component/Assets/Roselie.ico"))};
  var frame=new DockPanel{Background=window.Background};var panel=new StackPanel{Margin=new Thickness(32,24,32,18)};
  var header=new StackPanel{Margin=new Thickness(32,22,32,20)};header.Children.Add(new Image{Source=new BitmapImage(new Uri("pack://application:,,,/Roselie.Setup;component/Assets/logo.png")),Width=116,Height=54,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left});header.Children.Add(new TextBlock{Text="Install Roselie's POS",FontSize=28,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,14,0,5)});header.Children.Add(new TextBlock{Text="Beauty Lounge · POS & Inventory · Version 0.2.1",FontSize=13,Foreground=rose});var headerSurface=new Border{Background=Brushes.White,BorderBrush=line,BorderThickness=new Thickness(0,0,0,1),Child=header};DockPanel.SetDock(headerSurface,Dock.Top);frame.Children.Add(headerSurface);
  panel.Children.Add(new TextBlock{Text="Ready for offline use on Windows 10/11 (64-bit). The runtime and dependencies are included. Choose Install, then open the POS from your desktop or Start Menu.",TextWrapping=TextWrapping.Wrap,FontSize=14,Margin=new Thickness(0,0,0,20)});panel.Children.Add(new TextBlock{Text="Installation folder",FontSize=12,Foreground=rose});var path=new TextBox{Text=defaultPath,Padding=new Thickness(10),Margin=new Thickness(0,8,0,18),FontSize=13,BorderBrush=line};panel.Children.Add(path);var desktop=new CheckBox{Content="Create desktop shortcut",IsChecked=true,Margin=new Thickness(0,0,0,14),FontSize=14};panel.Children.Add(desktop);panel.Children.Add(new TextBlock{Text="Business data is stored separately. Updates keep existing salon records.",TextWrapping=TextWrapping.Wrap,FontSize=12,Foreground=rose});var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,16,0),VerticalAlignment=VerticalAlignment.Center,FontSize=12};var install=new Button{Content="Install",Padding=new Thickness(22,10,22,10),MinWidth=110,MinHeight=40,Background=rose,Foreground=Brushes.White,BorderThickness=new Thickness(0),FontWeight=FontWeights.SemiBold,IsDefault=true};
  string? installedDestination=null;
  install.Click+=async (_,_)=>
  {
   if(installedDestination!=null){System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(installedDestination,"Roselie.POS.exe")){UseShellExecute=true});window.Close();return;}
   install.IsEnabled=false;
   try
   {
    var destination=Path.GetFullPath(path.Text);ValidateDestination(destination);status.Text="Installing and verifying application files…";
    await Task.Run(()=>Install(destination,desktop.Dispatcher.Invoke(()=>desktop.IsChecked==true)));
    installedDestination=destination;path.IsEnabled=false;desktop.IsEnabled=false;status.Text="Installed successfully. Open the POS to sign in or set up your owner account.";install.Content="Open Roselie's POS";install.IsEnabled=true;
   }
   catch(Exception ex){status.Text=ex.GetBaseException().Message;install.IsEnabled=true;}
  };
  var footerGrid=new Grid();footerGrid.ColumnDefinitions.Add(new ColumnDefinition());footerGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});footerGrid.Children.Add(status);Grid.SetColumn(install,1);footerGrid.Children.Add(install);var footer=new Border{Child=footerGrid,Padding=new Thickness(32,18,32,22),Background=Brushes.White,BorderBrush=line,BorderThickness=new Thickness(0,1,0,0)};DockPanel.SetDock(footer,Dock.Bottom);frame.Children.Add(footer);frame.Children.Add(new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});window.Content=frame;if(uiSmoke){window.Left=-20000;window.Top=0;}window.Show();
  if(uiSmoke)Dispatcher.BeginInvoke(()=>{try{window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)frame.ActualWidth,(int)frame.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(frame);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));var output=Path.GetFullPath(e.Args[1]);Directory.CreateDirectory(Path.GetDirectoryName(output)!);using var stream=File.Create(output);encoder.Save(stream);Shutdown(0);}catch{Shutdown(2);}},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
 }
 private static void ValidateDestination(string path)
 {
  var root=Path.GetPathRoot(path);if(string.Equals(path.TrimEnd(Path.DirectorySeparatorChar),root?.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Choose an application subfolder.");
  if(Directory.Exists(path)&&Directory.EnumerateFileSystemEntries(path).Any())
  {
   if(!File.Exists(Path.Combine(path,"install-manifest.json")))throw new InvalidOperationException("Choose an empty folder or an existing Roselie installation.");
   ReadManifest(path);
  }
  var data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RoselieBeautyLounge");if(path.StartsWith(data,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Choose a folder separate from the business database.");
 }
 private static Manifest ReadManifest(string directory)
 {
  Manifest manifest;
  try{manifest=JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(directory,"install-manifest.json")))??throw new InvalidDataException("Installation manifest is empty.");}
  catch(JsonException ex){throw new InvalidDataException("This folder does not contain a valid Roselie installation manifest. Choose an empty folder.",ex);}
  if(manifest.Product!=ProductId||manifest.Files==null||manifest.Files.Count==0)throw new InvalidDataException("This folder belongs to another application or its installation manifest is invalid. Choose an empty folder.");
  foreach(var item in manifest.Files){ResolvePayloadPath(directory,item.Key);if(item.Value==null||item.Value.Length!=64||!item.Value.All(Uri.IsHexDigit))throw new InvalidDataException("Installation manifest contains an invalid file checksum.");}
  return manifest;
 }
 private static string ResolvePayloadPath(string directory,string relative)
 {
  var prefix=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
  var target=Path.GetFullPath(Path.Combine(directory,relative));
  if(string.IsNullOrWhiteSpace(relative)||!target.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unsafe installation file path.");
  return target;
 }
 private static void PreflightFiles(IEnumerable<string> paths,string? runningExecutable=null)
 {
  foreach(var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
  {
   if(!File.Exists(path)||string.Equals(path,runningExecutable,StringComparison.OrdinalIgnoreCase))continue;
   try{using var access=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None);}
   catch(IOException ex){throw new IOException("Close Roselie's POS and any open files in its installation folder, then try again. Windows could not exclusively access: "+Path.GetFileName(path),ex);}
   catch(UnauthorizedAccessException ex){throw new UnauthorizedAccessException("Windows cannot update "+Path.GetFileName(path)+". Close Roselie's POS, check folder permissions, and try again.",ex);}
  }
 }
 private static IEnumerable<string> ShortcutPaths(bool desktop)
 {
  yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"Roselie's Beauty Lounge.lnk");
  if(desktop)yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Roselie's Beauty Lounge.lnk");
 }
 private static void ExtractAndVerify(string destination)
 {
  using var payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("Roselie.Payload.zip")??throw new InvalidOperationException("Installer payload is missing. Build using scripts/publish.ps1.");using var archive=new ZipArchive(payload,ZipArchiveMode.Read);var prefix=Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
  foreach(var entry in archive.Entries){var target=Path.GetFullPath(Path.Combine(destination,entry.FullName));if(!target.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unsafe payload path.");if(string.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(target);continue;}Directory.CreateDirectory(Path.GetDirectoryName(target)!);entry.ExtractToFile(target,true);}
  var manifest=ReadManifest(destination);foreach(var item in manifest.Files){var target=ResolvePayloadPath(destination,item.Key);var actual=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target)));if(actual!=item.Value)throw new InvalidDataException("Payload verification failed: "+item.Key);}
 }
 private static void Install(string destination,bool desktop)
 {
  // Verify the complete archive before changing an existing installation, then keep rollback copies.
  ValidateDestination(destination);
  var staging=Path.Combine(Path.GetTempPath(),"Roselie-Install-"+Guid.NewGuid().ToString("N"));
  var payload=Path.Combine(staging,"payload");var rollback=Path.Combine(staging,"rollback");
  var changed=false;var preserveRollback=false;var backups=new Dictionary<string,string?>(StringComparer.OrdinalIgnoreCase);
  var executable=Path.GetFullPath(Environment.ProcessPath!);var uninstall=Path.Combine(destination,"Uninstall-Roselie.exe");
  try
  {
   Directory.CreateDirectory(payload);ExtractAndVerify(payload);var manifest=ReadManifest(payload);
   var previous=File.Exists(Path.Combine(destination,"install-manifest.json"))?ReadManifest(destination):null;
   var relativeFiles=manifest.Files.Keys.Concat(previous?.Files.Keys??Enumerable.Empty<string>()).Append("install-manifest.json").Append("Uninstall-Roselie.exe").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
   var paths=relativeFiles.Select(relative=>ResolvePayloadPath(destination,relative)).ToList();
   var shortcuts=ShortcutPaths(desktop).ToList();PreflightFiles(paths.Concat(shortcuts),executable);
   foreach(var relative in relativeFiles)
   {
    var target=ResolvePayloadPath(destination,relative);if(string.Equals(target,executable,StringComparison.OrdinalIgnoreCase))continue;
    if(File.Exists(target)){var backup=ResolvePayloadPath(rollback,relative);Directory.CreateDirectory(Path.GetDirectoryName(backup)!);File.Copy(target,backup,true);backups[target]=backup;}
    else backups[target]=null;
   }
   for(var i=0;i<shortcuts.Count;i++){var target=shortcuts[i];if(File.Exists(target)){var backup=Path.Combine(rollback,"shortcuts",i+".lnk");Directory.CreateDirectory(Path.GetDirectoryName(backup)!);File.Copy(target,backup,true);backups[target]=backup;}else backups[target]=null;}
   Directory.CreateDirectory(destination);changed=true;
   foreach(var relative in manifest.Files.Keys.Append("install-manifest.json"))
   {
    var target=ResolvePayloadPath(destination,relative);Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(ResolvePayloadPath(payload,relative),target,true);
   }
   if(previous!=null)foreach(var relative in previous.Files.Keys.Except(manifest.Files.Keys,StringComparer.OrdinalIgnoreCase)){var target=ResolvePayloadPath(destination,relative);if(!string.Equals(target,executable,StringComparison.OrdinalIgnoreCase)&&File.Exists(target))File.Delete(target);}
   if(!string.Equals(executable,uninstall,StringComparison.OrdinalIgnoreCase))File.Copy(executable,uninstall,true);
   var desktopShortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Roselie's Beauty Lounge.lnk");
   foreach(var shortcut in shortcuts)CreateShortcut(shortcut,Path.Combine(destination,"Roselie.POS.exe"),string.Equals(shortcut,desktopShortcut,StringComparison.OrdinalIgnoreCase));
   using var key=Registry.CurrentUser.CreateSubKey(UninstallKey);key.SetValue("DisplayName","Roselie's Beauty Lounge POS");key.SetValue("DisplayVersion","0.2.1");key.SetValue("DisplayIcon",Path.Combine(destination,"Roselie.POS.exe")+",0");key.SetValue("Publisher","Roselie's Beauty Lounge");key.SetValue("InstallLocation",destination);key.SetValue("UninstallString","\""+uninstall+"\" --uninstall");key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);
  }
  catch(Exception installError)
  {
   if(changed)
   {
    try{foreach(var item in backups){if(item.Value!=null)File.Copy(item.Value,item.Key,true);else if(File.Exists(item.Key))File.Delete(item.Key);}}
    catch(Exception rollbackError){preserveRollback=true;throw new AggregateException("Installation could not finish restoring the previous files. Close Roselie's POS and preserve the recovery copies in "+rollback+".",installError,rollbackError);}
   }
   throw;
  }
  finally{if(!preserveRollback&&Directory.Exists(staging)){try{Directory.Delete(staging,true);}catch(IOException){}catch(UnauthorizedAccessException){}}}
 }
 private static void CreateShortcut(string path,string target,bool desktopIcon)
 {
  var type=Type.GetTypeFromProgID("WScript.Shell")??throw new InvalidOperationException("Windows shortcut service unavailable.");dynamic shell=Activator.CreateInstance(type)!;try{dynamic shortcut=shell.CreateShortcut(path);try{shortcut.TargetPath=target;shortcut.WorkingDirectory=Path.GetDirectoryName(target);shortcut.IconLocation=(desktopIcon?Path.Combine(Path.GetDirectoryName(target)!,"Assets","Roselie.Desktop.ico"):target)+",0";shortcut.Description="Roselie's Beauty Lounge POS";shortcut.Save();}finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);}}finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
 }
 private void Uninstall()
 {
  try
  {
   using var key=Registry.CurrentUser.OpenSubKey(UninstallKey);var directory=key?.GetValue("InstallLocation") as string??throw new InvalidOperationException("Installation not registered.");var manifestPath=Path.Combine(directory,"install-manifest.json");var manifest=ReadManifest(directory);if(MessageBox.Show("Uninstall Roselie's POS? Your business database and backups will be kept.","Uninstall",MessageBoxButton.YesNo)!=MessageBoxResult.Yes){Shutdown();return;}
   var executable=Path.GetFullPath(Environment.ProcessPath!);var paths=manifest.Files.Keys.Select(relative=>ResolvePayloadPath(directory,relative)).Append(manifestPath).Concat(ShortcutPaths(true)).ToList();PreflightFiles(paths,executable);
   foreach(var target in paths){if(!string.Equals(target,executable,StringComparison.OrdinalIgnoreCase)&&File.Exists(target))File.Delete(target);}Registry.CurrentUser.DeleteSubKeyTree(UninstallKey,false);MessageBox.Show("Application removed. Your business data was preserved. The installer executable can be deleted after it closes.");Shutdown();
  }
  catch(Exception ex){MessageBox.Show(ex.GetBaseException().Message,"Uninstall could not finish");Shutdown(1);}
 }
}
