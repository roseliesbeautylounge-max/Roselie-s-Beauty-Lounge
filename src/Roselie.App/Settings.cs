using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Roselie.Core.Models;
using Roselie.App.Management;

namespace Roselie.App;

public sealed partial class MainWindow
{
 private Func<bool>? _settingsDirty;
 private Action<string>? _smokeSettingsSection;
 internal void SmokeSettingsSection(string section)=>_smokeSettingsSection?.Invoke(section);
 private FrameworkElement BuildSettings()
 {
  var layout = new Grid();
  layout.ColumnDefinitions.Add(new(){Width=new GridLength(174)});
  layout.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  var navigation = new StackPanel();
  var navTitle=Ui.Text("PREFERENCES",10,"#9A8790",true);navTitle.Margin=new Thickness(12,8,0,14);navigation.Children.Add(navTitle);
  var content = new ContentControl { Margin=new Thickness(24,0,0,0),HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch };
  Grid.SetColumn(content,1);layout.Children.Add(content);
  layout.Children.Add(Ui.Scroll(navigation));
  var sections=new Dictionary<string,(FrameworkElement Body,Button Button,TextBlock State)>();
  _smokeSettingsSection=section=>{if(sections.TryGetValue(section,out var selected))selected.Button.Command?.Execute(null);};
  var dirty=new HashSet<string>();
  _settingsDirty=()=>dirty.Count>0;
  void MarkSaved(string section){dirty.Remove(section);if(sections.TryGetValue(section,out var saved)){saved.State.Text="Saved locally";saved.State.Foreground=Ui.Brush("#27845F");}}
  void Tab(string section, UIElement body)
  {
   var state=Ui.Text("Changes are stored on this computer",11,"#77727B");state.Margin=new Thickness(0,0,0,12);
   var bounded=new Border{MaxWidth=860,Child=body,HorizontalAlignment=HorizontalAlignment.Stretch};
   var scroll=Ui.Scroll(bounded);
   var frame=new Grid();frame.RowDefinitions.Add(new(){Height=GridLength.Auto});frame.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});frame.RowDefinitions.Add(new(){Height=GridLength.Auto});frame.Children.Add(state);Grid.SetRow(scroll,1);frame.Children.Add(scroll);
   var saveActions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
   void ExtractSave(DependencyObject root){foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>().ToList()){if(child is Button save && save.Content is string label && label.StartsWith("Save ") && save.Parent is Panel parent){parent.Children.Remove(save);save.Margin=new Thickness(0);saveActions.Children.Add(save);}else ExtractSave(child);}}
   ExtractSave(body);
   if(saveActions.Children.Count>0){var footer=new Border{Padding=new Thickness(16,12,16,0),Child=saveActions,Background=Ui.Brush("#FAF6F5")};Grid.SetRow(footer,2);frame.Children.Add(footer);}
   void Select(){content.Content=frame;foreach(var item in sections)item.Value.Button.Background=Ui.Brush(item.Key==section?"#F3E7E7":"#FAF6F5");Ui.Enter(bounded);}
   var button=Ui.Button(section,Select);button.Style=(Style)FindResource("NavigationButton");button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,0,0,5);
   sections[section]=(frame,button,state);navigation.Children.Add(button);
   void Changed(){dirty.Add(section);state.Text="● Unsaved changes";state.Foreground=Ui.Brush("#C88B35");}
   void Watch(DependencyObject root){if(root is TextBox text)text.TextChanged+=(_,_)=>Changed();else if(root is PasswordBox password)password.PasswordChanged+=(_,_)=>Changed();else if(root is ComboBox combo)combo.SelectionChanged+=(_,_)=>Changed();else if(root is CheckBox check){check.Checked+=(_,_)=>Changed();check.Unchecked+=(_,_)=>Changed();}foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())Watch(child);}
   Watch(body);
   if(sections.Count==1)Select();
  }

  // --- Business Information ---
  var name = Ui.Input(_business.GetSetting("BusinessName", "Roselie's Beauty Lounge"), 420);
  var address = Ui.Input(_business.GetSetting("BusinessAddress"), 420);
  var contact = Ui.Input(_business.GetSetting("BusinessContact"), 260);
  var registration = Ui.Input(_business.GetSetting("TaxRegistrationDetails"), 420);
  registration.AcceptsReturn = true;
  registration.MinHeight = 80;

  var logoBox = new Border
  {
   Background = Ui.Brush("#FAF6F5"),
   CornerRadius = new CornerRadius(10),
   Padding = new Thickness(14),
   Margin = new Thickness(0, 6, 0, 16),
   Width = 420,
   HorizontalAlignment = HorizontalAlignment.Left
  };
  var logoRow = new Grid();
  logoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
  logoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
  var logoPreview = Ui.CreateLogoImage(60, 60);
  logoPreview.Margin = new Thickness(0, 0, 14, 0);
  logoRow.Children.Add(logoPreview);
  var logoText = Ui.Stack(
   Ui.Text("Official Salon Branding", 13, "#344052", true),
   Ui.Text("Your salon identity in the navigation and receipt preview.", 12, "#77727B")
  );
  var logoActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
  var uploadLogoBtn = Ui.Button("Upload logo", () =>
  {
   var ofd = new OpenFileDialog { Title = "Select Salon Logo", Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp" };
   if (ofd.ShowDialog() == true)
   {
    var candidate=new System.Windows.Media.Imaging.BitmapImage();candidate.BeginInit();candidate.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;candidate.UriSource=new Uri(ofd.FileName);candidate.EndInit();
    if(candidate.PixelWidth>10000||candidate.PixelHeight>10000)throw new InvalidOperationException("Choose a logo smaller than 10,000 pixels per side.");
    var dest = Path.Combine(_database.DataDirectory, "custom-logo" + Path.GetExtension(ofd.FileName).ToLowerInvariant());
    File.Copy(ofd.FileName, dest, true);
    _business.SetSetting("LogoPath", dest, _user!);
    Ui.CustomLogoPath = dest;
    logoPreview.Source = Ui.CreateLogoImage(60, 60).Source;
    Ui.NotifyLogoChanged();
    Ui.Toast("Salon logo updated successfully!");
   }
  });
  uploadLogoBtn.Padding = new Thickness(12, 6, 12, 6);
  uploadLogoBtn.MinHeight = 32;

  var resetLogoBtn = Ui.Button("Reset", () =>
  {
   _business.SetSetting("LogoPath", "", _user!);
   Ui.CustomLogoPath = null;
   logoPreview.Source = Ui.CreateLogoImage(60, 60).Source;
   Ui.NotifyLogoChanged();
   Ui.Toast("Reset to default logo");
  });
  resetLogoBtn.Padding = new Thickness(10, 6, 10, 6);
  resetLogoBtn.MinHeight = 32;

  logoActions.Children.Add(uploadLogoBtn);
  logoActions.Children.Add(resetLogoBtn);
  logoText.Children.Add(logoActions);
  Grid.SetColumn(logoText, 1);
  logoRow.Children.Add(logoText);
  logoBox.Child = logoRow;

  var businessSaveBtn = Ui.Button("Save business details", () =>
  {
   if (string.IsNullOrWhiteSpace(name.Text)) throw new InvalidOperationException("Enter the business name.");
   _business.SetSetting("BusinessName", name.Text.Trim(), _user!);
   _business.SetSetting("BusinessAddress", address.Text.Trim(), _user!);
   _business.SetSetting("BusinessContact", contact.Text.Trim(), _user!);
   _business.SetSetting("TaxRegistrationDetails", registration.Text.Trim(), _user!);
   MarkSaved("Business");
   Ui.Toast("Business information saved successfully");
  }, true);
  businessSaveBtn.HorizontalAlignment = HorizontalAlignment.Right;
  businessSaveBtn.Margin = new Thickness(0, 12, 0, 0);

  var businessForm = Ui.Stack(
   Ui.Text("Business Profile & Receipt Details", 20, "#344052", true),
   Ui.Text("Receipts use these details as entered. Tax rules and official invoicing require applicable business review before use.", 13, "#77727B"),
   Ui.Field("Salon Brand Logo", logoBox),
   Ui.Field("Salon legal or trade name", name),
   Ui.Field("Contact number", contact),
   Ui.Field("Address line", address),
   Ui.Field("Taxpayer / registration details", registration),
   businessSaveBtn
  );
  businessForm.MaxWidth = 620;
  businessForm.HorizontalAlignment = HorizontalAlignment.Left;
  Tab("Business", Ui.Card(businessForm, new Thickness(0)));

  // --- Printer ---
  var printer = new ComboBox { Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
  const string automaticPrinter="Automatic receipt printer";
  var savedPrinter=_business.GetSetting("PrinterName");
  var width = new ComboBox { ItemsSource = ReceiptPrinterSupport.PaperWidths, SelectedItem = double.TryParse(_business.GetSetting("ReceiptWidth", "80"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var mm)&&ReceiptPrinterSupport.PaperWidths.Contains(mm)?mm:80d, Width = 140, HorizontalAlignment = HorizontalAlignment.Left };
  var automaticPaper=new CheckBox{Content="Automatically use the detected printer's paper profile",IsChecked=_business.GetSetting("ReceiptPaperMode","Auto")=="Auto",Margin=new Thickness(0,8,0,12)};
  var printerStatus=Ui.Text("",13,"#77727B");var printerDetails=Ui.Text("",12,"#77727B");
  void RefreshPrinters()
  {
   try
   {
    var selectedName=printer.SelectedItem?.ToString()??savedPrinter;
    var found=ReceiptPrinterSupport.Discover();var options=new List<string>{automaticPrinter};options.AddRange(found.Select(p=>p.Name));
    if(selectedName.Length>0&&selectedName!=automaticPrinter&&!options.Contains(selectedName))options.Add(selectedName);
    if(!options.SequenceEqual(printer.Items.Cast<string>())){printer.ItemsSource=options;printer.SelectedItem=options.Contains(selectedName)?selectedName:automaticPrinter;}
    var selected=ReceiptPrinterSupport.Select(printer.SelectedItem?.ToString()==automaticPrinter?"":printer.SelectedItem?.ToString()??"",found);
    printerStatus.Text=selected==null?"No receipt printer detected. Connect the printer and install its Windows driver.":selected.Name+" · "+selected.Status;
    printerStatus.Foreground=Ui.Brush(selected?.IsReady==true?"#27845F":"#A85E67");
    printerDetails.Text=selected?.IsEpsonU220==true?"Epson TM-U220 impact printer · 57.5 / 69.5 / 76 mm paper. Automatic uses 76 mm; choose the actual roll width if different.":selected==null?"Automatic selects a single Epson TM-U220, your default physical printer, or the only physical printer.":"Driver: "+selected.Driver;
   }
   catch(Exception ex){printerStatus.Text="Windows printer detection: "+ex.GetBaseException().Message;}
  }
  void UpdatePaper(){width.IsEnabled=automaticPaper.IsChecked!=true;}
  automaticPaper.Checked+=(_,_)=>UpdatePaper();automaticPaper.Unchecked+=(_,_)=>UpdatePaper();UpdatePaper();
  printer.SelectionChanged+=(_,_)=>RefreshPrinters();RefreshPrinters();

  var printerActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
  var testPrintBtn = Ui.Button("Test print receipt", () =>
  {
   var print = new PrintDialog();ReceiptPrinterSupport.ConfigureDialog(print,printer.SelectedItem?.ToString()==automaticPrinter?"":printer.SelectedItem?.ToString()??"");
   if (print.ShowDialog() == true)
   {
    var selected=ReceiptPrinterSupport.RequireReady(print.PrintQueue.FullName);var paperWidth=ReceiptPrinterSupport.ResolveWidth(Convert.ToString(width.SelectedItem,System.Globalization.CultureInfo.InvariantCulture)??"80",automaticPaper.IsChecked==true,selected.Name+" "+selected.Driver);
    var document = ReceiptDocument("ROSELIE'S BEAUTY LOUNGE\nPrinter test\n--------------------\nSample service     PHP 500.00\nDiscount 10%        PHP 50.00\nTOTAL             PHP 450.00\n\nThank you!",paperWidth);
    print.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator, "Roselie printer test");
    Ui.Toast("Sent test receipt to printer");
   }
  });
  var savePrinterBtn = Ui.Button("Save printer settings", () =>
  {
   _business.SetSetting("PrinterName", printer.SelectedItem?.ToString()==automaticPrinter?"":printer.SelectedItem?.ToString()??"", _user!);
   _business.SetSetting("ReceiptWidth", Convert.ToString(width.SelectedItem,System.Globalization.CultureInfo.InvariantCulture)??"80", _user!);
   _business.SetSetting("ReceiptPaperMode",automaticPaper.IsChecked==true?"Auto":"Manual",_user!);
   MarkSaved("Printer");
   Ui.Toast("Printer settings saved");
  }, true);
  printerActions.Children.Add(testPrintBtn);
  printerActions.Children.Add(savePrinterBtn);

  var printerForm = Ui.Stack(
   Ui.Text("Receipt Printer", 20, "#344052", true),
   Ui.Text("Connect the printer and install its Windows driver once. Installed printers are detected automatically; Windows printing supports Epson impact and thermal receipt printers.", 13, "#77727B"),
   Ui.Field("Installed receipt printer", printer),
   printerStatus,printerDetails,Ui.Button("Refresh printers",RefreshPrinters),automaticPaper,
   Ui.Field("Paper roll width (mm)", width),
   Ui.Text("Set the same paper width in the printer driver and paper guide. Windows status depends on the driver; print a test receipt to confirm paper, ribbon and alignment.",12,"#77727B"),
   printerActions
  );
  printerForm.MaxWidth = 620;
  printerForm.HorizontalAlignment = HorizontalAlignment.Left;
  var printerTimer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(5)};printerTimer.Tick+=(_,_)=>RefreshPrinters();printerForm.Loaded+=(_,_)=>printerTimer.Start();printerForm.Unloaded+=(_,_)=>printerTimer.Stop();
  Tab("Printer", Ui.Card(printerForm, new Thickness(0)));

  // --- Pricing Policies ---
  var pricingForm = Ui.Stack(
   Ui.Text("Salon Pricing Policies", 20, "#344052", true),
   Ui.Text("Money rounds to two decimal places, midpoint away from zero. Additional charges apply per unit. Choose item discounts or a transaction discount; stacking both is strictly rejected.", 14, "#344052"),
   Ui.Text("Custom prices and discount limits are assigned to individual login accounts in Employees. Services contains configurable materials, price rules, charge presets and package offers.", 14, "#77727B"),
   Ui.Row(
    Ui.Button("Manage services & charge presets", () => Navigate("Services"), true),
    Ui.Button("Manage account permissions", () => Navigate("Employees"))
   )
  );
  pricingForm.Children[3].SetValue(MarginProperty, new Thickness(0, 16, 0, 0));
  pricingForm.MaxWidth = 680;
  pricingForm.HorizontalAlignment = HorizontalAlignment.Left;
  Tab("Pricing", Ui.Card(pricingForm, new Thickness(0)));

  // --- Database & Backups ---
  var backupDirectory = Ui.Input(_business.GetSetting("BackupDirectory", System.IO.Path.Combine(_database.DataDirectory, "Backups")), 460);
  var interval = Ui.Input(_business.GetSetting("BackupScheduleHours", "24"), 120);
  var passphrase = new PasswordBox { Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
  var databaseStatus = Ui.Badge("● SQLCipher encrypted · Saved on this PC", "#EAF5F0", "#27845F", 12);
  databaseStatus.Margin = new Thickness(0, 8, 0, 12);
  var databaseFile=Ui.Input(_database.DatabasePath);databaseFile.IsReadOnly=true;databaseFile.ToolTip=_database.DatabasePath;System.Windows.Automation.AutomationProperties.SetName(databaseFile,"Database file location");
  var databaseLocation=new Grid();databaseLocation.ColumnDefinitions.Add(new ColumnDefinition());databaseLocation.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});databaseLocation.Children.Add(databaseFile);var copyDatabasePath=Ui.IconButton("\uE8C8","Copy database file path",()=>{Clipboard.SetText(_database.DatabasePath);Ui.Toast("Database path copied");});copyDatabasePath.Margin=new Thickness(10,0,0,0);Grid.SetColumn(copyDatabasePath,1);databaseLocation.Children.Add(copyDatabasePath);

  var databaseActions = new WrapPanel { Margin = new Thickness(0, 16, 0, 16) };
  databaseActions.Children.Add(Ui.Button("Save backup schedule", () =>
  {
   if (!int.TryParse(interval.Text, out var hours) || hours < 0 || hours > 8760) throw new InvalidOperationException("Backup interval must be between 0 and 8760 hours.");
   if (string.IsNullOrWhiteSpace(backupDirectory.Text)) throw new InvalidOperationException("Choose a backup folder.");
   if (passphrase.Password.Length > 0 && passphrase.Password.Length < 12) throw new InvalidOperationException("Use at least 12 characters for the backup passphrase.");
   _business.SetSetting("BackupDirectory", System.IO.Path.GetFullPath(backupDirectory.Text), _user!);
   _business.SetSetting("BackupScheduleHours", hours.ToString(), _user!);
   if (passphrase.Password.Length > 0)
    _business.SetSetting("BackupPassphraseProtected", Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(passphrase.Password), null, DataProtectionScope.CurrentUser)), _user!);
   MarkSaved("Database");
   Ui.Toast("Backup schedule saved");
  }, true));
  databaseActions.Children.Add(Ui.Button("Create encrypted portable backup", () =>
  {
   var save = new SaveFileDialog { FileName = "Roselie-" + SalonTime.Now.ToString("yyyyMMdd-HHmm") + ".rblbackup", Filter = "Encrypted Roselie backup|*.rblbackup" };
   if (save.ShowDialog() == true)
   {
    var record = _backups.CreateBackup(save.FileName, passphrase.Password, _user!);
    MessageBox.Show("Backup created and verified:\n" + record.FilePath);
    Ui.Toast("Encrypted backup created");
   }
  }));
  databaseActions.Children.Add(Ui.Button("Verify backup", () =>
  {
   var open = new OpenFileDialog { Filter = "Encrypted Roselie backup|*.rblbackup" };
   if (open.ShowDialog() == true)
    MessageBox.Show(_backups.VerifyBackup(open.FileName, passphrase.Password, _user!) ? "Backup is valid." : "Backup verification failed.");
  }));
  databaseActions.Children.Add(Ui.Button("Restore encrypted backup", () =>
  {
   var open = new OpenFileDialog { Filter = "Encrypted Roselie backup|*.rblbackup" };
   if (open.ShowDialog() != true) return;
   if (!Ui.Confirm("Restore database", "Restore this verified backup and replace the current database? A safety copy will be kept. Sign in again after restoration.", "Restore backup", true)) return;
   _backups.RestoreBackup(open.FileName, passphrase.Password, _user!);
   _auth.Logout();
   _user = null;
   _pos = null;
   ShowLogin("Backup restored. Sign in to the restored account.");
  }));
  databaseActions.Children.Add(Ui.Button("Check database integrity", () =>
  {
   MessageBox.Show(_database.CheckIntegrity() ? "Database integrity check passed." : "Database integrity check failed. Preserve your files and restore a verified backup.");
  }));

  var history = _business.List<BackupRecord>(_user!).OrderByDescending(b => b.CreatedAtUtc).Take(20).Select(b => new { Date = SalonTime.FromUtc(b.CreatedAtUtc).ToString("dd MMM yyyy HH:mm"), File = System.IO.Path.GetFileName(b.FilePath), Bytes = (b.SizeBytes / 1024.0).ToString("N1") + " KB" });

  var databaseForm = Ui.Stack(
   Ui.Text("Encrypted Database & Automated Backups", 20, "#344052", true),
   databaseStatus,
   Ui.Field("Database file location",databaseLocation),
   Ui.Text("Saved records remain after closing the app or restarting this PC. Keep salon.db and database.key together in this data folder. Use portable backup/restore when moving records to another PC.",13,"#77727B"),
   Ui.Field("Scheduled backup destination (local drive or USB)", backupDirectory),
   Ui.Field("Backup interval in hours (0 disables)", interval),
   Ui.Field("Backup passphrase (12+ characters for portable backups)", passphrase),
   databaseActions,
   Ui.Text("Backup History", 16, "#344052", true),
   Ui.Table(history, ("Date", "Date"), ("Filename", "File"), ("Size", "Bytes"))
  );
  databaseForm.MaxWidth = 840;
  databaseForm.HorizontalAlignment = HorizontalAlignment.Left;
  Tab("Database", Ui.Card(databaseForm, new Thickness(0)));

  // --- Appearance ---
  var rose = Ui.Input(_business.GetSetting("ThemeRose", "#C8797D"), 140);
  var density = new ComboBox { ItemsSource = new[] { "Comfortable", "Compact" }, SelectedItem = _business.GetSetting("TableDensity", "Comfortable"), Width = 180, HorizontalAlignment = HorizontalAlignment.Left };
  var reduceMotion=new CheckBox{Content="Reduce interface motion",IsChecked=_business.GetSetting("ReduceMotion","false")=="true",Margin=new Thickness(0,0,0,16)};
  var appearanceSaveBtn = Ui.Button("Save appearance", () =>
  {
   if (!System.Text.RegularExpressions.Regex.IsMatch(rose.Text, "^#[0-9A-Fa-f]{6}$")) throw new InvalidOperationException("Enter a six-digit hex color such as #C8797D.");
   var brush = Ui.Brush(rose.Text);
   _business.SetSetting("ThemeRose", rose.Text, _user!);
   _business.SetSetting("TableDensity", density.SelectedItem?.ToString() ?? "Comfortable", _user!);
   _business.SetSetting("ReduceMotion",reduceMotion.IsChecked==true?"true":"false",_user!);Ui.ReduceMotion=reduceMotion.IsChecked==true;
   Application.Current.Resources["RoseBrush"] = brush;
   var style = (Style)Application.Current.FindResource("ModernDataGrid");
   var newStyle = new Style(typeof(DataGrid), style);
   newStyle.Setters.Add(new Setter(DataGrid.RowHeightProperty, density.SelectedItem?.ToString() == "Compact" ? 34d : 44d));
   Application.Current.Resources[typeof(DataGrid)] = newStyle;
   MarkSaved("Appearance");
   Ui.Toast("Appearance settings saved");
  }, true);
  appearanceSaveBtn.HorizontalAlignment = HorizontalAlignment.Right;

  var appearanceForm = Ui.Stack(
   Ui.Text("Salon Brand Theme & Layout", 20, "#344052", true),
   Ui.Field("Primary accent rose color (#RRGGBB)", rose),
   Ui.Field("Table display density", density),
   reduceMotion,
   Ui.Text("Standard Segoe UI typography and Windows DPI scaling keep text readable across monitors. Choose Compact for higher information density.", 13, "#77727B"),
   appearanceSaveBtn
  );
  appearanceForm.MaxWidth = 620;
  appearanceForm.HorizontalAlignment = HorizontalAlignment.Left;
  Tab("Appearance", Ui.Card(appearanceForm, new Thickness(0)));

  var users=_business.List<UserAccount>(_user!).Where(u=>u.IsActive).ToList();
  var sessionMinutes=Ui.Input(((int)_auth.SessionTimeout.TotalMinutes).ToString(),100);var failedAttempts=Ui.Input(_auth.FailedAttemptLimit.ToString(),100);var lockoutMinutes=Ui.Input(((int)_auth.LockoutDuration.TotalMinutes).ToString(),100);
  var accountActions=new WrapPanel{Margin=new Thickness(0,0,0,14)};foreach(var button in new[]{Ui.Button("My profile",ShowProfileSettings),Ui.Button("Change my password",ShowPasswordChange,true),Ui.Button("Manage my sign-in PIN",ShowPinSettings),Ui.Button("Manage Super PIN",()=>ManagementPageFactory.ShowSuperPinSettings(_auth,_user!)),Ui.Button("Add employee account",()=>ManagementPageFactory.ShowEmployeeAccountCreation(_business,_user!,_auth)),Ui.Button("Employee login accounts",()=>ManagementPageFactory.ShowAccountManagement(_business,_user!,_auth))}){button.Margin=new Thickness(0,0,12,12);accountActions.Children.Add(button);}
  var security=Ui.Stack(Ui.Text("Users & account security",20,"#344052",true),Ui.Text($"{users.Count} active account(s). Create employee login accounts and assign roles and pricing permissions.",13,"#77727B"),Ui.Text("Sessions lock after your configured inactivity period. Enroll an account PIN for quick sign-in on this computer. Password access stays available.",13,"#77727B"),accountActions,Ui.Button("Employee directory",()=>Navigate("Employees")));
  foreach(FrameworkElement child in security.Children)child.Margin=new Thickness(0,0,0,14);
  security.Children.Add(Ui.Field("Session timeout (1–120 minutes)",sessionMinutes));security.Children.Add(Ui.Field("Lock account after failed attempts (3–10)",failedAttempts));security.Children.Add(Ui.Field("Lockout duration (5–1,440 minutes)",lockoutMinutes));
  security.Children.Add(Ui.Button("Save security settings",()=>{if(!int.TryParse(sessionMinutes.Text,out var timeout)||timeout is <1 or >120)throw new InvalidOperationException("Set a session timeout from 1 to 120 minutes.");if(!int.TryParse(failedAttempts.Text,out var attempts)||attempts is <3 or >10)throw new InvalidOperationException("Set a failed-attempt limit from 3 to 10.");if(!int.TryParse(lockoutMinutes.Text,out var lockout)||lockout is <5 or >1440)throw new InvalidOperationException("Set a lockout from 5 to 1,440 minutes.");_business.SetSetting("Security.SessionTimeoutMinutes",timeout.ToString(),_user!);_business.SetSetting("Security.LockoutAttempts",attempts.ToString(),_user!);_business.SetSetting("Security.LockoutMinutes",lockout.ToString(),_user!);MarkSaved("User security");Ui.Toast("Security settings saved");},true));
  Tab("User security",Ui.Card(security,new Thickness(0)));

  // --- Audit Log ---
  var logs = _business.List<AuditLog>(_user!).OrderByDescending(a => a.OccurredAtUtc).Take(1000).Select(a => new { Date = SalonTime.FromUtc(a.OccurredAtUtc).ToString("dd MMM yyyy HH:mm:ss"), Action = a.Action, Entity = a.EntityType, Details = a.Details });
  var auditForm = Ui.Stack(
   Ui.Text("System Audit Trail", 20, "#344052", true),
   Ui.Text("Recorded security, financial, and management events (latest 1,000 entries).", 13, "#77727B"),
   Ui.Table(logs, ("Timestamp", "Date"), ("Action", "Action"), ("Module", "Entity"), ("Details", "Details"))
  );
  ((FrameworkElement)auditForm.Children[^1]).Height=420;
  Tab("Audit log", Ui.Card(auditForm, new Thickness(0)));

  return layout;
 }

 private void ShowPasswordChange()
 {
  if(_user==null)return;_auth.Require(_user,true);
  var current=new PasswordBox();var password=new PasswordBox{MaxLength=256};var confirmation=new PasswordBox{MaxLength=256};
  System.Windows.Automation.AutomationProperties.SetName(current,"Current password");System.Windows.Automation.AutomationProperties.SetName(password,"New password");System.Windows.Automation.AutomationProperties.SetName(confirmation,"Confirm new password");
  var error=Ui.Text("",13,"#C84D58");var body=Ui.Stack(Ui.Text("Change the password for "+_user.Username,18,"#344052",true),Ui.Text("Use 12–256 characters. Saving signs you out and removes your sign-in PIN and Super PIN; you can enroll them again after signing in with your new password.",13,"#77727B"),Ui.Field("Current password",current),Ui.Field("New password",password),Ui.Field("Confirm new password",confirmation),error);body.Children[1].SetValue(MarginProperty,new Thickness(0,10,0,20));
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};var dialog=Ui.Dialog("Change administrator password",body,550,580,actions);var busy=false;dialog.Closing+=(_,e)=>{if(busy)e.Cancel=true;};var cancel=Ui.Button("Cancel",()=>dialog.Close());cancel.Margin=new Thickness(0,0,12,0);cancel.IsCancel=true;actions.Children.Add(cancel);
  var save=Ui.Button("Change password",()=>{},true);save.Command=null;save.IsDefault=true;save.Margin=new Thickness(0);actions.Children.Add(save);
  save.Click+=async(_,_)=>
  {
   if(busy)return;error.Text="";
   try
   {
    if(password.Password!=confirmation.Password)throw new InvalidOperationException("The new password entries do not match.");var account=_user!;var oldPassword=current.Password;var nextPassword=password.Password;busy=true;body.IsEnabled=false;actions.IsEnabled=false;save.Content="Saving…";
    await Task.Run(()=>_auth.ChangeOwnPassword(oldPassword,nextPassword,account));
    current.Clear();password.Clear();confirmation.Clear();busy=false;dialog.Close();_user=null;_pos=null;ShowLogin("Password changed. Sign in using your new password.");
   }
   catch(Exception ex){error.Text=ex.GetBaseException().Message;}
   finally{busy=false;body.IsEnabled=true;actions.IsEnabled=true;save.Content="Change password";}
  };
  dialog.ShowDialog();
 }
 internal void SmokePasswordDialog()=>ShowPasswordChange();
 private void ShowProfileSettings()
 {
  if(_user==null)return;_auth.Require(_user,true);var displayName=Ui.Input(string.IsNullOrWhiteSpace(_user.DisplayName)?_user.Username:_user.DisplayName);displayName.MaxLength=100;System.Windows.Automation.AutomationProperties.SetName(displayName,"Administrator display name");var username=Ui.Input(_user.Username);username.IsReadOnly=true;var error=Ui.Text("",13,"#C84D58");
  var body=Ui.Stack(Ui.Text("Administrator profile",18,"#344052",true),Ui.Text("Choose the name shown in your account profile and sidebar.",13,"#77727B"),Ui.Field("Display name",displayName),Ui.Field("Sign-in username",username),Ui.Text("Your username stays the same for password sign-in. Use your personal PIN without a username.",12,"#77727B"),error);body.Children[1].SetValue(MarginProperty,new Thickness(0,10,0,20));
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};var dialog=Ui.Dialog("My profile",body,530,470,actions);var cancel=Ui.Button("Cancel",()=>dialog.Close());cancel.Margin=new Thickness(0,0,12,0);cancel.IsCancel=true;actions.Children.Add(cancel);var save=Ui.Button("Save profile",()=>{try{_auth.UpdateOwnProfile(displayName.Text,_user!);RefreshProfileName();dialog.Close();Ui.Toast("Profile updated");}catch(Exception ex){error.Text=ex.GetBaseException().Message;}},true);save.IsDefault=true;save.Margin=new Thickness(0);actions.Children.Add(save);dialog.ShowDialog();
 }

 private void ShowPinSettings()
 {
  if(_user==null)return;_auth.Require(_user);
  var enrolled=_auth.IsPinEnabled(_user);
  var currentPassword=new PasswordBox();var pin=new PasswordBox{MaxLength=12};var confirmation=new PasswordBox{MaxLength=12};
  System.Windows.Automation.AutomationProperties.SetName(currentPassword,"Current account password");System.Windows.Automation.AutomationProperties.SetName(pin,"New sign-in PIN");System.Windows.Automation.AutomationProperties.SetName(confirmation,"Confirm sign-in PIN");
  var error=Ui.Text("",13,"#C84D58");var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
  var body=Ui.Stack(Ui.Text("Sign-in PIN",24,"#344052",true),Ui.Badge(enrolled?"PIN enabled on this computer":"PIN not enrolled",enrolled?"#EAF5F0":"#F3E7E7",enrolled?"#27845F":"#A85E67"),Ui.Text("Use 6–12 digits for quick offline sign-in. Your PIN belongs to this account and Windows profile. Use your password after moving a backup to another computer.",13,"#77727B"),Ui.Field("Confirm your current password",currentPassword),Ui.Field("New PIN (6–12 digits)",pin),Ui.Field("Confirm new PIN",confirmation),error);
  body.Children[1].SetValue(MarginProperty,new Thickness(0,14,0,12));body.Children[2].SetValue(MarginProperty,new Thickness(0,0,0,20));
  var dialog=Ui.Dialog("Sign-in PIN",body,530,620,actions);var busy=false;dialog.Closing+=(_,e)=>{if(busy)e.Cancel=true;};
  var cancel=Ui.Button("Cancel",()=>dialog.Close());cancel.IsCancel=true;
  var save=Ui.Button(enrolled?"Update PIN":"Enable PIN",()=>{},true);save.IsDefault=true;
  async Task Apply(bool disable)
  {
   if(busy)return;error.Text="";
   try
   {
    if(!disable&&pin.Password!=confirmation.Password)throw new InvalidOperationException("The PIN entries do not match.");
    var currentUser=_user!;var accountPassword=currentPassword.Password;var nextPin=pin.Password;
    busy=true;actions.IsEnabled=false;body.IsEnabled=false;save.Content="Saving…";
    await Task.Run(()=>{if(disable)_auth.DisablePin(accountPassword,currentUser);else _auth.EnrollPin(nextPin,accountPassword,currentUser);});
    busy=false;currentPassword.Clear();pin.Clear();confirmation.Clear();dialog.Close();Ui.Toast(disable?"PIN sign-in disabled":"PIN ready for your next sign-in");
   }
   catch(Exception ex){error.Text=ex.GetBaseException().Message;}
   finally{busy=false;actions.IsEnabled=true;body.IsEnabled=true;save.Content=enrolled?"Update PIN":"Enable PIN";}
  }
  save.Command=null;save.Click+=async(_,_)=>await Apply(false);actions.Children.Add(cancel);
  if(enrolled){var remove=Ui.DangerButton("Disable PIN",()=>{});remove.Command=null;remove.Click+=async(_,_)=>await Apply(true);actions.Children.Add(remove);}
  actions.Children.Add(save);dialog.ShowDialog();
 }
}

