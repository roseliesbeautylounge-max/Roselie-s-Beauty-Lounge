using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Finance;
using Roselie.Core.Models;
using Roselie.App.Management;
using Roselie.Infrastructure;

namespace Roselie.App;

public partial class App : Application
{
 private ServiceProvider? _services;
 private Mutex? _mutex;
 private string? _smokeOutput;
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  DispatcherUnhandledException += (_,args)=>{if(_smokeOutput!=null){File.WriteAllText(Path.Combine(_smokeOutput,"smoke-results.json"),JsonSerializer.Serialize(new{Success=false,Error=args.Exception.ToString()}));Shutdown(2);}else MessageBox.Show(args.Exception.GetBaseException().Message,"Roselie's Beauty Lounge",MessageBoxButton.OK,MessageBoxImage.Error);args.Handled=true;};
  try
  {
   string? smokeOutput=null;
   var verifyPersistence=e.Args.Length==2&&e.Args[0]=="--verify-persistence";
   if(e.Args.Length==2 && (e.Args[0]=="--ui-smoke"||verifyPersistence)) smokeOutput=Path.GetFullPath(e.Args[1]);
   _smokeOutput=smokeOutput;if(smokeOutput!=null)Directory.CreateDirectory(smokeOutput);
   var dataPath=smokeOutput==null?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RoselieBeautyLounge"):Path.Combine(smokeOutput,"isolated-data");
   var mutexName="Local\\RoseliePOS-"+Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dataPath)))[..20];
   _mutex=new Mutex(true,mutexName,out var isNew);
   if(!isNew) { MessageBox.Show("Roselie's POS is already running for this data folder.");Shutdown();return; }
   // Acquire exclusive app ownership before creating or loading the encryption key.
   var db=new DatabaseService(dataPath);
   db.Initialize();
   if(verifyPersistence){Shutdown(VerifyPersistence(db,smokeOutput!)?0:2);return;}
   _services=new ServiceCollection().AddSingleton(db).AddSingleton<AuthService>().AddSingleton<BusinessService>().AddSingleton<BackupService>().BuildServiceProvider();
   var auth=_services.GetRequiredService<AuthService>();
   var business=_services.GetRequiredService<BusinessService>();
   var backups=_services.GetRequiredService<BackupService>();
   var window=new MainWindow(db,auth,business,backups);
   MainWindow=window;if(smokeOutput!=null){window.Left=-20000;window.Top=0;}else window.StartFullScreen();window.Show();
   if(smokeOutput==null)
   {
    var backupTimer=new DispatcherTimer{Interval=TimeSpan.FromMinutes(1)};
    var running=false;
    backupTimer.Tick+=async (_,_)=>{if(running)return;running=true;try{var backup=await Task.Run(()=>backups.RunScheduledIfDue());if(backup!=null){var failurePath=Path.Combine(db.DataDirectory,"backup-status.txt");if(File.Exists(failurePath))File.Delete(failurePath);}}catch(Exception ex){File.WriteAllText(Path.Combine(db.DataDirectory,"backup-status.txt"),$"{SalonTime.Now:yyyy-MM-dd HH:mm} {ex.GetBaseException().Message}");}finally{running=false;}};
    backupTimer.Start();
   }
   if(smokeOutput!=null) Dispatcher.BeginInvoke(async ()=>await Smoke(window,auth,smokeOutput),DispatcherPriority.ApplicationIdle);
  }
  catch(Exception ex) {if(_smokeOutput!=null)File.WriteAllText(Path.Combine(_smokeOutput,"smoke-results.json"),JsonSerializer.Serialize(new{Success=false,Error=ex.ToString()},new JsonSerializerOptions{WriteIndented=true}));else MessageBox.Show("The application could not start.\n\n"+ex.GetBaseException().Message,"Roselie's Beauty Lounge",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1); }
 }
 private async Task Smoke(MainWindow window,AuthService auth,string output)
 {
  var screens=new List<string>();
  try
  {
   Directory.CreateDirectory(output);
   window.Width=1366;window.Height=768;
   await Task.Delay(250);
   Capture(window,Path.Combine(output,"01-first-run.png"));screens.Add("First-run administrator setup");
   var password="Smoke!"+Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
   if(auth.FirstRunRequired)auth.CreateFirstAdmin("smoke-owner",password);
   else throw new InvalidOperationException("UI smoke requires a fresh isolated-data folder.");
   window.SmokeSignIn();await Task.Delay(250);Capture(window,Path.Combine(output,"02-sign-in.png"));screens.Add("Sign in");
   var user=auth.Login("smoke-owner",password);
   window.ShowShell(user);
   var index=3;
   foreach(var page in Roselie.App.MainWindow.Pages)
   {
    window.Navigate(page);await Task.Delay(260);window.UpdateLayout();
    Capture(window,Path.Combine(output,$"{index++:00}-{page.Replace(' ','-')}.png"));screens.Add(page);
   }
   foreach(var size in new[]{(Width:1600d,Height:900d),(Width:1920d,Height:1080d)})
   {
    window.Width=size.Width;window.Height=size.Height;
    foreach(var page in new[]{"Dashboard","Point of Sale","Reports","Settings"}){window.Navigate(page);await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-{page.Replace(' ','-')}-{size.Width:0}x{size.Height:0}.png"));screens.Add(page+$" {size.Width:0}x{size.Height:0}");}
   }
   window.Width=1080;window.Height=700;
   foreach(var page in new[]{"Point of Sale","Appointments","Settings"}){window.Navigate(page);await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-{page.Replace(' ','-')}-compact.png"));screens.Add(page+" compact");}
   window.Width=1366;window.Height=768;window.Navigate("Settings");
   foreach(var section in new[]{"Printer","Pricing","Database","Appearance","User security","Audit log"}){window.SmokeSettingsSection(section);await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-Settings-{section.Replace(' ','-')}.png"));screens.Add("Settings "+section);}
   window.Navigate("Point of Sale");window.SmokeToggleSidebar();await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-POS-icon-rail.png"));screens.Add("Sidebar icon rail");window.SmokeToggleSidebar();await Task.Delay(260);
   window.Navigate("Appointments");window.UpdateLayout();
   foreach(var mode in new[]{"Month","Week","Day"}){window.SmokeCalendarView(mode);await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-Appointments-{mode}.png"));screens.Add("Appointments "+mode);}
   window.Navigate("Point of Sale");
   Ui.SmokeDialogObserver=dialog=>{Capture(dialog,Path.Combine(output,$"{index++:00}-dialog-{dialog.Title.Split(' ')[0]}.png"));screens.Add(dialog.Title);};
   window.SmokePosDialogs();window.SmokeHistory();window.SmokeManagementDialogs();window.SmokePinDialog();
   _services!.GetRequiredService<BusinessService>().Save(new Employee{FullName="Smoke Employee",Position="Stylist"},user);
   Ui.SmokeDialogObserver=dialog=>
   {
    Capture(dialog,Path.Combine(output,$"{index++:00}-dialog-{dialog.Title.Split(' ')[0]}.png"));screens.Add(dialog.Title);
    var elements=VisualDescendants(dialog).ToArray();
    if(dialog.Title=="My profile")
    {
     elements.OfType<TextBox>().Single(t=>AutomationProperties.GetName(t)=="Administrator display name").Text="Roselie Smoke Owner";
     elements.OfType<Button>().Single(b=>b.Content?.ToString()=="Save profile").Command!.Execute(null);
    }
    if(dialog.Title=="Add employee account")
    {
     elements.OfType<ComboBox>().Single(c=>AutomationProperties.GetName(c)=="Active employee, required").SelectedIndex=0;
     elements.OfType<TextBox>().Single(t=>AutomationProperties.GetName(t)=="Username, required").Text="smoke-stylist";
     elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetName(t)=="Password, required").Password="Employee!Smoke2026";
     elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetName(t)=="Confirm password, required").Password="Employee!Smoke2026";
     elements.OfType<Button>().Single(b=>b.Content?.ToString()=="Save changes").Command!.Execute(null);
    }
   };
   window.SmokeProfileDialog();if(auth.CurrentUser?.DisplayName!="Roselie Smoke Owner")throw new InvalidOperationException("Profile form did not save the display name.");
   window.SmokeAccountDialogs();
   if(!_services.GetRequiredService<BusinessService>().List<UserAccount>(user).Any(a=>a.Username=="smoke-stylist"&&a.EmployeeId.HasValue))throw new InvalidOperationException("Employee account form did not create the linked login.");
   var nextPassword="Changed!"+Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
   Ui.SmokeDialogObserver=dialog=>
   {
    Capture(dialog,Path.Combine(output,$"{index++:00}-dialog-password-change.png"));screens.Add(dialog.Title);
    var elements=VisualDescendants(dialog).ToArray();
    elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetName(t)=="Current password").Password=password;
    elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetName(t)=="New password").Password=nextPassword;
    elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetName(t)=="Confirm new password").Password=nextPassword;
    elements.OfType<Button>().Single(b=>b.Content?.ToString()=="Change password").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   };
   window.SmokePasswordDialog();if(auth.CurrentUser!=null)throw new InvalidOperationException("Password form must end the previous session.");password=nextPassword;user=auth.Login("smoke-owner",password);window.ShowShell(user);
   Ui.SmokeDialogObserver=dialog=>
   {
    Capture(dialog,Path.Combine(output,$"{index++:00}-dialog-Super-PIN.png"));screens.Add(dialog.Title);var elements=VisualDescendants(dialog).ToArray();
    elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetAutomationId(t)=="SuperPin.CurrentPassword").Password=password;
    elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetAutomationId(t)=="SuperPin.NewPin").Password="59184726";
    elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetAutomationId(t)=="SuperPin.ConfirmPin").Password="59184726";
    elements.OfType<Button>().Single(b=>AutomationProperties.GetAutomationId(b)=="SuperPin.Save").Command!.Execute(null);
   };
   ManagementPageFactory.SmokeSuperPinDialog(auth,user);if(!auth.IsSuperPinEnabled(user))throw new InvalidOperationException("Super PIN form did not enroll the protected deletion credential.");
   var smokeBusiness=_services.GetRequiredService<BusinessService>();var smokeExpense=smokeBusiness.Save(new Expense{Description="Smoke deletion expense",Category="Test",Amount=10,RecordedByUserId=user.Id},user);
   Ui.SmokeDialogObserver=dialog=>
   {
    Capture(dialog,Path.Combine(output,$"{index++:00}-dialog-{dialog.Title.Replace(' ','-')}.png"));screens.Add(dialog.Title);var elements=VisualDescendants(dialog).ToArray();
    elements.OfType<TextBox>().Single(t=>AutomationProperties.GetAutomationId(t)=="Deletion.Reason").Text="Isolated smoke verification";
    elements.OfType<PasswordBox>().Single(t=>AutomationProperties.GetAutomationId(t)=="Deletion.SuperPin").Password="59184726";
    elements.OfType<Button>().Single(b=>AutomationProperties.GetAutomationId(b)=="Deletion.Confirm").Command!.Execute(null);
   };
   ManagementPageFactory.SmokeDeletionDialogs(smokeBusiness,user,auth);
   using(var deleted= _services.GetRequiredService<DatabaseService>().OpenContext()){if(!deleted.Set<SalonService>().Any(s=>s.IsDeleted)||!deleted.Set<PackageOffer>().Any(p=>p.IsDeleted)||!deleted.Set<Expense>().Single(e=>e.Id==smokeExpense.Id).IsVoided)throw new InvalidOperationException("Protected deletion forms did not preserve and mark their records.");}
   Ui.SmokeDialogObserver=dialog=>{Capture(dialog,Path.Combine(output,$"{index++:00}-dialog-{dialog.Title.Split(' ')[0]}.png"));screens.Add(dialog.Title);};
   foreach(var mode in new[]{CalendarMode.Month,CalendarMode.Year,CalendarMode.Decade}){var calendar=new Calendar{SelectedDate=SalonTime.Today,DisplayDate=SalonTime.Today,DisplayMode=mode};var calendarDialog=Ui.Dialog("Choose a date · "+mode,calendar,430,490);calendarDialog.ShowDialog();}Ui.SmokeDialogObserver=null;
   window.SmokeReceiptPrinting(output);screens.Add("57.5 / 58 / 69.5 / 76 / 80 mm receipt pagination, saved values and legacy fallback");
   window.Hide();window.StartFullScreen();if(window.WindowStyle!=WindowStyle.None||window.WindowState!=WindowState.Maximized)throw new InvalidOperationException("Full-screen entry failed.");auth.Logout(user);window.SmokeSignIn();if(window.WindowStyle!=WindowStyle.None||window.WindowState!=WindowState.Maximized)throw new InvalidOperationException("Sign-out lost full-screen mode.");user=auth.Login("smoke-owner",password);window.ShowShell(user);window.SmokeFullScreen();if(window.WindowStyle!=WindowStyle.SingleBorderWindow||window.WindowState!=WindowState.Normal)throw new InvalidOperationException("Full-screen restoration failed.");window.Left=-20000;window.Top=0;window.Show();screens.Add("Automatic full-screen startup, sign-out retention and restoration");
   window.Navigate("Dashboard");await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-Dashboard-with-sale.png"));screens.Add("Dashboard with committed sale");
   auth.EnrollPin("846291",password,user);auth.Logout(user);window.SmokeSignIn(true);await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-PIN-login.png"));screens.Add("PIN-only number pad with username hidden");
   var loginElements=VisualDescendants(window).ToArray();if(loginElements.OfType<TextBox>().Any(t=>AutomationProperties.GetName(t)=="Username"&&t.IsVisible))throw new InvalidOperationException("Username is visible in PIN-only mode.");
   foreach(var digit in "846291")loginElements.OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="PIN digit "+digit).Command!.Execute(null);
   ((UIElement)window.Content).RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window)!,Environment.TickCount,Key.Enter){RoutedEvent=Keyboard.PreviewKeyDownEvent});
   for(var retry=0;auth.CurrentUser==null&&retry<100;retry++)await Task.Delay(50);user=auth.CurrentUser??throw new InvalidOperationException("Number-pad sign-in did not complete.");await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-PIN-login-success.png"));screens.Add("Number-pad PIN sign-in after sign-out without username");
   for(var cycle=0;cycle<2;cycle++){auth.Logout(user);window.SmokeSignIn();await Task.Delay(100);user=auth.Login("smoke-owner",password);window.ShowShell(user);await Task.Delay(260);Capture(window,Path.Combine(output,$"{index++:00}-repeated-login-{cycle+1}.png"));}screens.Add("Repeated password sign-out and sign-in without reused-parent errors");
   using(var context=_services!.GetRequiredService<DatabaseService>().OpenContext())File.WriteAllText(Path.Combine(output,"database-schema.sql"),context.Database.GenerateCreateScript());
   WritePersistenceExpectation(_services!.GetRequiredService<DatabaseService>(),output);
   File.WriteAllText(Path.Combine(output,"smoke-results.json"),JsonSerializer.Serialize(new{Success=true,Screens=screens},new JsonSerializerOptions{WriteIndented=true}));
   Shutdown(0);
  }
  catch(Exception ex) {Ui.SmokeDialogObserver=null; File.WriteAllText(Path.Combine(output,"smoke-results.json"),JsonSerializer.Serialize(new{Success=false,Screens=screens,Error=ex.ToString()},new JsonSerializerOptions{WriteIndented=true}));Shutdown(2); }
 }
 private static IEnumerable<DependencyObject> VisualDescendants(DependencyObject root)
 {
  yield return root;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in VisualDescendants(VisualTreeHelper.GetChild(root,i)))yield return child;
 }
 private static void Capture(Window window,string path)
 {
  window.UpdateLayout();if(window is MainWindow main)main.SmokeSettleAnimations();var visual=window.Template?.FindName("DialogRoot",window) as FrameworkElement??(FrameworkElement)window.Content;var bitmap=new RenderTargetBitmap((int)visual.ActualWidth,(int)visual.ActualHeight,96,96,PixelFormats.Pbgra32);var background=new DrawingVisual();using(var drawing=background.RenderOpen())drawing.DrawRectangle(window.Background,null,new Rect(0,0,visual.ActualWidth,visual.ActualHeight));bitmap.Render(background);bitmap.Render(visual);
  var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(path);encoder.Save(stream);
 }
 protected override void OnExit(ExitEventArgs e) { _services?.Dispose();_mutex?.Dispose();base.OnExit(e); }
}
