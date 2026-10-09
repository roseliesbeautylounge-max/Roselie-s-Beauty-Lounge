using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Roselie.Core.Models;
using Roselie.Infrastructure;
using Roselie.App.Management;

namespace Roselie.App;

public sealed partial class MainWindow : Window
{
 public static readonly string[] Pages=["Dashboard","Point of Sale","Services","Products","Inventory","Customers","Appointments","Employees","Expenses","Packages","Reports","Settings"];
 
 private static readonly (string Group, string[] Pages)[] NavGroups = [
  ("MAIN", ["Dashboard", "Point of Sale"]),
  ("MANAGEMENT", ["Services", "Products", "Inventory", "Customers", "Appointments", "Packages"]),
  ("BUSINESS", ["Employees", "Expenses", "Reports"]),
  ("SYSTEM", ["Settings"])
 ];

 private static readonly Dictionary<string, string> PageIcons = new()
 {
  ["Dashboard"] = "\uE80F",
  ["Point of Sale"] = "\uE719",
  ["Services"] = "\uE790",
  ["Products"] = "\uE7B8",
  ["Inventory"] = "\uE8A5",
  ["Customers"] = "\uE77B",
  ["Appointments"] = "\uE787",
  ["Packages"] = "\uE8D4",
  ["Employees"] = "\uE716",
  ["Expenses"] = "\uE8A7",
  ["Reports"] = "\uE9D2",
  ["Settings"] = "\uE713"
 };

 private readonly DatabaseService _database;
 private readonly AuthService _auth;
 private readonly BusinessService _business;
 private readonly BackupService _backups;
 private UserAccount? _user;
 private ContentControl _page=new();
 private TextBlock _title=new();
 private TextBlock _clock=new();
 private StackPanel _nav=new();
 private readonly Dictionary<string,Button> _navButtons=new();
 private readonly StackPanel _toastPanel=new(){HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,16,28,0)};
 private DateTime _lastInput=DateTime.UtcNow;
 private bool _collapsed;
 private string _activePage="";
 private bool _fullScreen;
 private WindowState _savedWindowState;
 private WindowStyle _savedWindowStyle;
 private ResizeMode _savedResizeMode;
 private Button? _fullScreenButton;
 private Grid? _shell;
 private PosView? _pos;
 private Image? _sidebarLogo;
 private TextBlock? _profileName;
 private TextBlock? _profileInitial;

 public MainWindow(DatabaseService database,AuthService auth,BusinessService business,BackupService backups)
 {
  _database=database;_auth=auth;_business=business;_backups=backups;
  Title="Roselie's Beauty Lounge · POS & Inventory";Width=Math.Min(1440,SystemParameters.WorkArea.Width);Height=Math.Min(900,SystemParameters.WorkArea.Height);MinWidth=Math.Min(1080,SystemParameters.WorkArea.Width);MinHeight=Math.Min(700,SystemParameters.WorkArea.Height);WindowStartupLocation=WindowStartupLocation.CenterScreen;
  Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Roselie.POS;component/Assets/Roselie.ico",UriKind.Absolute));
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.F11){ToggleFullScreen();e.Handled=true;}else if(e.Key==Key.Escape&&_fullScreen){ToggleFullScreen();e.Handled=true;}};
  void Activity(){_lastInput=DateTime.UtcNow;if(_user!=null){try{_auth.Touch(_user);}catch(UnauthorizedAccessException){_user=null;_pos=null;ShowLogin("Your session expired. Sign in to continue.");}}}
  InputManager.Current.PreProcessInput+=(_,args)=>{if(args.StagingItem.Input is MouseButtonEventArgs or KeyEventArgs or TextCompositionEventArgs)Activity();};
  var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(15)};
  timer.Tick+=(_,_)=> { _clock.Text=SalonTime.Now.ToString("ddd, dd MMM yyyy   h:mm tt"); if(_user!=null && DateTime.UtcNow-_lastInput>_auth.SessionTimeout){_auth.Logout(_user);_user=null;_pos=null;ShowLogin("Your session expired. Sign in to continue.");} };
  timer.Start();
  Ui.OnToast += (msg, type) => ShowToast(msg, type);
  Ui.OnLogoChanged += () => { if (_sidebarLogo != null) _sidebarLogo.Source = Ui.CreateLogoImage(44, 44).Source; };
  ShowLogin();
 }

 public void ShowToast(string message, string type = "success")
 {
  Dispatcher.InvokeAsync(() =>
  {
   var bgHex = type switch
   {
    "info" => "#344052",
    "warning" => "#C88B35",
    "danger" => "#C84D58",
    _ => "#27845F"
   };
   var icon = type switch
   {
    "info" => "ℹ",
    "warning" => "⚠",
    "danger" => "✕",
    _ => "✓"
   };
   var toastCard = new Border
   {
    Background = Ui.Brush(bgHex),
    CornerRadius = new CornerRadius(10),
    Padding = new Thickness(16, 11, 14, 11),
    Margin = new Thickness(0, 0, 0, 8),
    MaxWidth = 420,
    Effect = new DropShadowEffect { BlurRadius = 16, Direction = 270, ShadowDepth = 3, Opacity = 0.25, Color = Color.FromRgb(30, 20, 25) }
   };
   var grid = new Grid();
   grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

   var iconBlock = new TextBlock { Text = icon, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
   var textBlock = new TextBlock { Text = message, FontSize = 13, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
   var closeBtn = new Button { Content = "✕", Foreground = Brushes.White, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(6, 2, 2, 2), Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center };

   Grid.SetColumn(iconBlock, 0);
   Grid.SetColumn(textBlock, 1);
   Grid.SetColumn(closeBtn, 2);
   grid.Children.Add(iconBlock);
   grid.Children.Add(textBlock);
   grid.Children.Add(closeBtn);
   toastCard.Child = grid;

   Ui.Fade(toastCard,0,1);

   void Dismiss()
   {
    Ui.Fade(toastCard,1,0,()=>_toastPanel.Children.Remove(toastCard));
   }
   closeBtn.Click += (_, _) => Dismiss();
   _toastPanel.Children.Add(toastCard);

   var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.8) };
   timer.Tick += (_, _) => { timer.Stop(); Dismiss(); };
   timer.Start();
  });
 }

 private void ShowLogin(string message="")
 {
  var grid=new Grid();
  grid.ColumnDefinitions.Add(new(){Width=new GridLength(1.15,GridUnitType.Star)});
  grid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});

  // Left Panel - Full Image Showcase
  var leftPanel = new Grid
  {
   Background = new SolidColorBrush(Color.FromRgb(15, 13, 17)),
   ClipToBounds = true
  };
  var leftHero = Ui.CreateLoginHeroImage();
  leftHero.Stretch = Stretch.UniformToFill;
  leftHero.HorizontalAlignment = HorizontalAlignment.Center;
  leftHero.VerticalAlignment = VerticalAlignment.Center;
  leftPanel.Children.Add(leftHero);
  grid.Children.Add(leftPanel);

  // Right Login Form Panel
  var first=_auth.FirstRunRequired;
  var usePin=false;
  var pinValue="";
  var username=Ui.Input();
  var password=new PasswordBox();
  var confirm=new PasswordBox();
  var visible=Ui.Input();
  visible.Visibility=Visibility.Collapsed;

  var passContainer=new Grid();
  passContainer.Children.Add(password);
  passContainer.Children.Add(visible);

  var toggle=new CheckBox{Content="Show password",Margin=new Thickness(0,0,0,12),Foreground=Ui.Brush("#77727B")};
  toggle.Checked+=(_,_)=>{visible.Text=password.Password;password.Visibility=Visibility.Collapsed;visible.Visibility=Visibility.Visible;};
  toggle.Unchecked+=(_,_)=>{password.Password=visible.Text;visible.Visibility=Visibility.Collapsed;password.Visibility=Visibility.Visible;};
  var credentialField=Ui.Field("Password",passContainer);
  System.Windows.Automation.AutomationProperties.SetName(password,"Sign-in credential");
  System.Windows.Automation.AutomationProperties.SetName(username,"Username");

  var remember=new CheckBox{Content="Remember username",Margin=new Thickness(0,0,0,18),Foreground=Ui.Brush("#77727B")};
  var rememberPath=Path.Combine(_database.DataDirectory,"remembered-username.txt");
  if(File.Exists(rememberPath)){username.Text=File.ReadAllText(rememberPath);remember.IsChecked=true;}

  var error=Ui.Text(message,13,"#C84D58");
  error.Margin=new Thickness(0,10,0,10);

  var usernameField=Ui.Field("Username",username);
  var pinDisplay=Ui.Text("Enter PIN",24,"#344052",true);pinDisplay.TextAlignment=TextAlignment.Center;
  System.Windows.Automation.AutomationProperties.SetName(pinDisplay,"Masked sign-in PIN");
  var keypad=new System.Windows.Controls.Primitives.UniformGrid{Columns=3,Rows=4,Margin=new Thickness(0,10,0,0)};
  var pinPanel=Ui.Stack(new Border{Background=Ui.Brush("#FAF6F5"),BorderBrush=Ui.Brush("#E9DADB"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Padding=new Thickness(12),Child=pinDisplay},keypad);pinPanel.Visibility=Visibility.Collapsed;
  void UpdatePinDisplay(){pinDisplay.Text=pinValue.Length==0?"Enter PIN":new string('●',pinValue.Length);}
  void PinKey(string key){if(key=="Clear")pinValue="";else if(key=="⌫"){if(pinValue.Length>0)pinValue=pinValue[..^1];}else if(pinValue.Length<12)pinValue+=key;UpdatePinDisplay();}
  foreach(var key in new[]{"1","2","3","4","5","6","7","8","9","Clear","0","⌫"}){var capturedKey=key;var button=Ui.Button(key,()=>PinKey(capturedKey));button.MinHeight=52;button.FontSize=key=="Clear"?14:22;button.Margin=new Thickness(4);System.Windows.Automation.AutomationProperties.SetName(button,key=="⌫"?"Delete last PIN digit":key=="Clear"?"Clear PIN":"PIN digit "+key);keypad.Children.Add(button);}
  var fields=Ui.Stack(
   Ui.Text(first?"Welcome to your salon":"Welcome back",32,"#344052",true),
   Ui.Text(first?"Create your administrator owner account to get started.":"Sign in to access Roselie's Beauty Lounge POS.",15,"#77727B"),
   usernameField,
   credentialField,
   toggle,pinPanel
  );
  fields.Children[1].SetValue(MarginProperty,new Thickness(0,8,0,26));
  if(!first)
  {
   Button? passwordMode=null;Button? pinMode=null;
   void Mode(bool pin){usePin=pin;password.Password="";visible.Text="";pinValue="";UpdatePinDisplay();usernameField.Visibility=credentialField.Visibility=toggle.Visibility=remember.Visibility=pin?Visibility.Collapsed:Visibility.Visible;pinPanel.Visibility=pin?Visibility.Visible:Visibility.Collapsed;passwordMode!.Style=(Style)FindResource(pin?"PillButton":"ActivePillButton");pinMode!.Style=(Style)FindResource(pin?"ActivePillButton":"PillButton");((TextBlock)fields.Children[1]).Text=pin?"Enter your personal PIN to sign in.":"Sign in to access Roselie's Beauty Lounge POS.";if(pin)((Button)keypad.Children[0]).Focus();else password.Focus();}
   passwordMode=Ui.Pill("Password",()=>Mode(false),true);pinMode=Ui.Pill("PIN",()=>Mode(true));passwordMode.Width=150;pinMode.Width=150;
   var modes=Ui.Row(passwordMode,pinMode);modes.Margin=new Thickness(0,0,0,14);fields.Children.Insert(2,modes);
  }

  if(first)
  {
   fields.Children.Add(Ui.Field("Confirm password",confirm));
   fields.Children.Add(Ui.Text("Use at least 12 characters. Keep your recovery code in a secure place.",13,"#77727B"));
  }
  else
  {
   fields.Children.Add(remember);
  }

  var signIn=new Button{Content=first?"Create owner account":"Sign in",Style=(Style)FindResource("PrimaryButton"),Margin=new Thickness(0,16,0,10),IsDefault=true,MinHeight=44,FontSize=15};
  signIn.Click+=async (_,_)=>
  {
   var pinSignIn=usePin;fields.IsEnabled=false;signIn.IsEnabled=false;keypad.IsEnabled=false;signIn.Content=first?"Creating your account…":"Signing in…";error.Text="";
   try
   {
    var name=username.Text.Trim();var pass=pinSignIn?pinValue:toggle.IsChecked==true?visible.Text:password.Password;
    if(first && pass!=confirm.Password)throw new InvalidOperationException("Passwords do not match.");
    if(first)
    {
     var recovery=await Task.Run(()=>_auth.CreateFirstAdmin(name,pass));
     var code=Ui.Input(recovery);code.IsReadOnly=true;
     var notice=Ui.Stack(Ui.Text("Save your recovery code",24,"#A85E67",true),Ui.Text("This code can reset your owner password while offline. It is shown once. Store it away from the salon computer.",15),Ui.Field("Recovery code",code));
     var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};var dialog=Ui.Dialog("Owner recovery code",notice,540,400,actions);var saved=Ui.Button("I saved my code",()=>dialog.Close(),true);saved.Margin=new Thickness(0);actions.Children.Add(saved);dialog.ShowDialog();
    }
    var user=await Task.Run(()=>pinSignIn?_auth.LoginWithPin(pass):_auth.Login(name,pass));
    if(!pinSignIn){if(remember.IsChecked==true)File.WriteAllText(rememberPath,name);else if(File.Exists(rememberPath))File.Delete(rememberPath);}
    ShowShell(user);
   }
   catch(Exception ex){error.Text=ex.GetBaseException().Message;pinValue="";UpdatePinDisplay();}
   finally{fields.IsEnabled=true;signIn.IsEnabled=true;keypad.IsEnabled=true;signIn.Content=first?"Create owner account":"Sign in";}
  };

  fields.Children.Add(signIn);
  fields.Children.Add(error);
  grid.PreviewKeyDown+=(_,e)=>{if(!usePin||!keypad.IsEnabled)return;if(e.Key>=Key.D0&&e.Key<=Key.D9){PinKey(((int)e.Key-(int)Key.D0).ToString());e.Handled=true;}else if(e.Key>=Key.NumPad0&&e.Key<=Key.NumPad9){PinKey(((int)e.Key-(int)Key.NumPad0).ToString());e.Handled=true;}else if(e.Key==Key.Back){PinKey("⌫");e.Handled=true;}else if(e.Key==Key.Delete){PinKey("Clear");e.Handled=true;}else if(e.Key==Key.Enter&&signIn.IsEnabled){e.Handled=true;signIn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}};

  if(!first)
  {
   var recoverBtn = Ui.Button("Recover owner account", ShowRecovery);
   recoverBtn.Margin = new Thickness(0, 4, 0, 16);
   recoverBtn.HorizontalAlignment = HorizontalAlignment.Left;
   fields.Children.Add(recoverBtn);
  }

  fields.Children.Add(Ui.Text("●   Fully offline · Encrypted local database",12,"#27845F"));

  fields.MaxWidth = 440;
  fields.Margin = new Thickness(56, 40, 56, 40);
  fields.VerticalAlignment = VerticalAlignment.Center;
  fields.HorizontalAlignment = HorizontalAlignment.Center;

  var right = new Grid
  {
   Background = Brushes.White,
   Children = { Ui.Scroll(fields) }
  };
  _fullScreenButton=Ui.IconButton("\uE740","Full screen (F11)",ToggleFullScreen);_fullScreenButton.Margin=new Thickness(0,0,8,0);UpdateFullScreenButton();
  var close=Ui.IconButton("\uE8BB","Close app",Close);close.Margin=new Thickness(0);var windowActions=Ui.Row(_fullScreenButton,close);windowActions.HorizontalAlignment=HorizontalAlignment.Right;windowActions.VerticalAlignment=VerticalAlignment.Top;windowActions.Margin=new Thickness(0,14,16,0);Panel.SetZIndex(windowActions,1);right.Children.Add(windowActions);
  Grid.SetColumn(right, 1);
  grid.Children.Add(right);

  Content=grid;
  _activePage="";_settingsDirty=null;
  grid.Loaded+=(_,_)=>{Ui.Enter(fields);if(username.Text.Length>0)password.Focus();else username.Focus();};
 }

 private void ShowRecovery()
 {
  var username=Ui.Input();var code=Ui.Input();var password=new PasswordBox();
  var form=Ui.Stack(Ui.Field("Owner username",username),Ui.Field("Recovery code",code),Ui.Field("New password",password));var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
  var dialog=Ui.Dialog("Offline account recovery",form,520,470,actions);actions.Children.Add(Ui.Button("Cancel",()=>dialog.Close()));
  actions.Children.Add(Ui.Button("Reset password",()=>{
   var next=_auth.RecoverAccount(username.Text,code.Text,password.Password);
   Clipboard.SetText(next);
   MessageBox.Show("Password reset. Your new recovery code was copied to the clipboard. Save it securely:\n\n"+next);
   dialog.Close();Ui.Toast("Password reset; save the new recovery code", "info");
  },true));
  dialog.ShowDialog();
 }

 public void ShowShell(UserAccount user)
 {
  _user=user;_lastInput=DateTime.UtcNow;_pos=null;_navButtons.Clear();_collapsed=false;_activePage="";_settingsDirty=null;
  try{Application.Current.Resources["RoseBrush"]=Ui.Brush(_business.GetSetting("ThemeRose","#C8797D"));}catch{}
  Ui.ReduceMotion=_business.GetSetting("ReduceMotion","false")=="true";
  var tableStyle=new Style(typeof(DataGrid),(Style)Application.Current.FindResource("ModernDataGrid"));
  tableStyle.Setters.Add(new Setter(DataGrid.RowHeightProperty,_business.GetSetting("TableDensity","Comfortable")=="Compact"?34d:44d));
  Application.Current.Resources[typeof(DataGrid)]=tableStyle;
  var logoPath=_business.GetSetting("LogoPath");Ui.CustomLogoPath=File.Exists(logoPath)?logoPath:null;
  _shell=new Grid();_shell.ColumnDefinitions.Add(new(){Width=GridLength.Auto});_shell.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  var side=new DockPanel{LastChildFill=true};
  var brand=Ui.Stack();_sidebarLogo=Ui.CreateLogoImage(150,43);brand.Children.Add(_sidebarLogo);
  var subtitle=Ui.Text("POS & INVENTORY",9,"#77727B",true);subtitle.TextAlignment=TextAlignment.Center;subtitle.Margin=new Thickness(0,7,0,0);brand.Children.Add(subtitle);
  var brandHeader=new Border{Padding=new Thickness(12,12,12,10),Child=brand};DockPanel.SetDock(brandHeader,Dock.Top);side.Children.Add(brandHeader);
  _profileInitial=new TextBlock{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,FontWeight=FontWeights.SemiBold,Foreground=Ui.Brush("#A85E67")};var avatar=new Border{Width=32,Height=32,CornerRadius=new CornerRadius(16),Background=Ui.Brush("#F3E7E7"),Child=_profileInitial};
  _profileName=Ui.Text("",13,"#344052",true);_profileName.TextWrapping=TextWrapping.NoWrap;_profileName.TextTrimming=TextTrimming.CharacterEllipsis;RefreshProfileName();
  void OpenProfile(){if(_auth.Require(_user!).Role==RoleKind.Administrator)ShowProfileSettings();else ShowPinSettings();}
  var account=Ui.Stack(_profileName,Ui.Text(user.Role==RoleKind.Administrator?"Salon owner":user.Role.ToString(),11,"#77727B"));account.Margin=new Thickness(10,0,8,0);account.ToolTip=user.Role==RoleKind.Administrator?"Manage your profile":"Manage your sign-in PIN";account.Cursor=Cursors.Hand;account.Focusable=true;KeyboardNavigation.SetIsTabStop(account,true);System.Windows.Automation.AutomationProperties.SetName(account,"Manage profile for "+user.Username);account.MouseLeftButtonUp+=(_,_)=>Ui.Guard(OpenProfile);account.KeyDown+=(_,e)=>{if(e.Key is Key.Enter or Key.Space){e.Handled=true;Ui.Guard(OpenProfile);}};avatar.ToolTip=account.ToolTip;avatar.Cursor=Cursors.Hand;avatar.MouseLeftButtonUp+=(_,_)=>Ui.Guard(OpenProfile);
  var signOut=Ui.IconButton("","Sign out",()=>{_auth.Logout(user);_user=null;_pos=null;ShowLogin();});signOut.Content=Ui.SignOutIcon();signOut.Margin=new Thickness(0);
  var footerRow=new Grid();footerRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});footerRow.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});footerRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});footerRow.Children.Add(avatar);Grid.SetColumn(account,1);footerRow.Children.Add(account);Grid.SetColumn(signOut,2);footerRow.Children.Add(signOut);
  var footer=new Border{Padding=new Thickness(16,16,12,16),BorderBrush=Ui.Brush("#E9DADB"),BorderThickness=new Thickness(0,1,0,0),Child=footerRow};DockPanel.SetDock(footer,Dock.Bottom);side.Children.Add(footer);
  _nav=new StackPanel{Margin=new Thickness(0,0,0,12)};
  UIElement NavContent(string page)
  {
   var row=new Grid();row.ColumnDefinitions.Add(new(){Width=new GridLength(22)});if(!_collapsed)row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
   var icon=Ui.Icon(PageIcons.GetValueOrDefault(page,"\uE80F"),17);row.Children.Add(icon);
   if(!_collapsed){var label=new TextBlock{Text=page,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(11,0,0,0)};Grid.SetColumn(label,1);row.Children.Add(label);}return row;
  }
  foreach(var group in NavGroups)
  {
   var visible=group.Pages.Where(p=>CanNavigate(p,user)).ToList();if(visible.Count==0)continue;
   var heading=Ui.Text(group.Group,9,"#9A8790",true);heading.Margin=new Thickness(24,8,12,3);_nav.Children.Add(heading);
   foreach(var page in visible){var button=Ui.Button("",()=>Navigate(page));button.Style=(Style)FindResource("NavigationButton");button.Content=NavContent(page);button.Margin=new Thickness(12,0,12,0);button.ToolTip=page;System.Windows.Automation.AutomationProperties.SetName(button,page);_navButtons[page]=button;_nav.Children.Add(button);}
  }
  side.Children.Add(Ui.Scroll(_nav));
  var sidebar=new Border{Width=232,ClipToBounds=true,Background=Brushes.White,BorderBrush=Ui.Brush("#E9DADB"),BorderThickness=new Thickness(0,0,1,0),Child=side};_shell.Children.Add(sidebar);
  var area=new DockPanel();var top=new Grid{Margin=new Thickness(24,16,24,16)};top.ColumnDefinitions.Add(new(){Width=GridLength.Auto});top.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});top.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  void ToggleRail()
  {
   _collapsed=!_collapsed;var target=_collapsed?72d:232d;var startWidth=sidebar.ActualWidth;sidebar.BeginAnimation(WidthProperty,null);sidebar.Width=target;
   if(Ui.MotionEnabled)sidebar.BeginAnimation(WidthProperty,new DoubleAnimation(startWidth,target,TimeSpan.FromMilliseconds(200)){EasingFunction=new QuadraticEase{EasingMode=EasingMode.EaseOut}});
   _sidebarLogo.Width=_collapsed?42:150;_sidebarLogo.Height=_collapsed?36:43;subtitle.Visibility=_collapsed?Visibility.Collapsed:Visibility.Visible;
   foreach(var heading in _nav.Children.OfType<TextBlock>())heading.Visibility=_collapsed?Visibility.Collapsed:Visibility.Visible;
   foreach(var pair in _navButtons){pair.Value.Content=NavContent(pair.Key);pair.Value.HorizontalContentAlignment=_collapsed?HorizontalAlignment.Center:HorizontalAlignment.Stretch;pair.Value.Padding=new Thickness(_collapsed?10:12,8, _collapsed?10:12,8);pair.Value.Margin=_collapsed?new Thickness(10,2,10,2):new Thickness(12,0,12,0);}
   avatar.Visibility=_collapsed?Visibility.Collapsed:Visibility.Visible;account.Visibility=_collapsed?Visibility.Collapsed:Visibility.Visible;footer.Padding=new Thickness(_collapsed?17:16,14,_collapsed?17:12,14);
  }
  var collapse=Ui.IconButton("\uE700","Expand or collapse navigation",ToggleRail);collapse.Margin=new Thickness(0,0,14,0);top.Children.Add(collapse);
  _title=Ui.Text("Dashboard",23,"#344052",true);_title.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(_title,1);top.Children.Add(_title);
  _clock=Ui.Text(SalonTime.Now.ToString("ddd, dd MMM yyyy   h:mm tt"),11,"#77727B");_clock.TextAlignment=TextAlignment.Right;
  var status=Ui.Stack(Ui.Badge("● Offline & ready","#EAF5F0","#27845F",10),_clock);status.Children[0].SetValue(HorizontalAlignmentProperty,HorizontalAlignment.Right);status.Children[1].SetValue(MarginProperty,new Thickness(0,4,0,0));
  _fullScreenButton=Ui.IconButton("\uE740","Full screen (F11)",ToggleFullScreen);_fullScreenButton.Margin=new Thickness(0,0,14,0);UpdateFullScreenButton();var topTools=Ui.Row(_fullScreenButton,status);Grid.SetColumn(topTools,2);top.Children.Add(topTools);DockPanel.SetDock(top,Dock.Top);area.Children.Add(top);
  _page=new ContentControl{Margin=new Thickness(24,0,24,22),HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch};area.Children.Add(_page);
  // The toast host survives sessions. Detach it from the previous shell before reuse.
  if(_toastPanel.Parent is Panel previousToastParent)previousToastParent.Children.Remove(_toastPanel);
  var main=new Grid();main.Children.Add(area);_toastPanel.Children.Clear();Panel.SetZIndex(_toastPanel,9999);main.Children.Add(_toastPanel);Grid.SetColumn(main,1);_shell.Children.Add(main);Content=_shell;
  Navigate(user.Role==RoleKind.SalonStaff?"Appointments":user.Role==RoleKind.Cashier?"Point of Sale":"Dashboard");
 }

 internal void SmokeToggleSidebar()
 {
  // Exercise the same accessible command used by the top-bar toggle.
  if(_shell==null)return;
  Button? Find(DependencyObject root){if(root is Button button && button.ToolTip?.ToString()=="Expand or collapse navigation")return button;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var match=Find(VisualTreeHelper.GetChild(root,i));if(match!=null)return match;}return null;}
  Find(_shell)?.Command?.Execute(null);
 }
 private static bool CanNavigate(string page,UserAccount user)=>user.Role==RoleKind.Administrator || user.Role==RoleKind.Cashier && page is "Point of Sale" or "Customers" or "Packages" || user.Role==RoleKind.SalonStaff && page is "Appointments" or "Customers";
 internal void StartFullScreen(){if(!_fullScreen)ToggleFullScreen();}
 private void ToggleFullScreen()
 {
  if(!_fullScreen){_savedWindowState=WindowState;_savedWindowStyle=WindowStyle;_savedResizeMode=ResizeMode;WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;WindowState=WindowState.Maximized;_fullScreen=true;}
  else{WindowState=WindowState.Normal;WindowStyle=_savedWindowStyle;ResizeMode=_savedResizeMode;WindowState=_savedWindowState;_fullScreen=false;}
  UpdateFullScreenButton();
 }
 private void UpdateFullScreenButton(){if(_fullScreenButton!=null){_fullScreenButton.Content=Ui.Icon(_fullScreen?"\uE73F":"\uE740");_fullScreenButton.ToolTip=_fullScreen?"Restore window (F11 or Esc)":"Full screen (F11)";System.Windows.Automation.AutomationProperties.SetName(_fullScreenButton,_fullScreenButton.ToolTip.ToString());}}
 private void RefreshProfileName(){if(_user==null)return;var name=string.IsNullOrWhiteSpace(_user.DisplayName)?_user.Username:_user.DisplayName;if(_profileName!=null){_profileName.Text=name;_profileName.ToolTip=name+" · "+_user.Username;}if(_profileInitial!=null)_profileInitial.Text=name[..1].ToUpperInvariant();}

 public void Navigate(string page)
 {
  if(_user==null)return;
  if(!CanNavigate(page,_user))throw new UnauthorizedAccessException("This page requires owner access.");
  if(_activePage=="Settings" && page!="Settings" && _settingsDirty?.Invoke()==true && !Ui.Confirm("Unsaved settings", "Your changes have not been saved. Leave Settings and discard these edits?", "Discard edits",true))return;
  _auth.Require(_user);
  _title.Text=page;_title.FontSize=23;_title.FontWeight=FontWeights.SemiBold;_title.Foreground=Ui.Brush("#344052");
  if(page is not ("Dashboard" or "Point of Sale" or "Reports" or "Settings")){_title.Text="Management  /  "+page;_title.FontSize=14;_title.FontWeight=FontWeights.Normal;_title.Foreground=Ui.Brush("#77727B");}

  foreach(var kv in _navButtons)
  {
   var isActive = kv.Key == page;
   kv.Value.Background = Ui.Brush(isActive ? "#F3E7E7" : "#FFFFFF");
   kv.Value.Foreground = Ui.Brush(isActive ? "#A85E67" : "#77727B");
   kv.Value.BorderBrush = Brushes.Transparent;
   kv.Value.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
  }

  _page.Content=page switch
  {
   "Dashboard"=>BuildDashboard(),
   "Point of Sale"=>OpenPos(),
   "Reports"=>BuildReports(),
   "Settings"=>BuildSettings(),
   _=>ManagementPageFactory.Create(page,_business,_user,_auth)
  };
  _activePage=page;

  // Smooth page transition
  Ui.Enter(_page);
 }

 private FrameworkElement OpenPos(){if(_pos==null)_pos=new PosView(_business,_user!,sale=>ShowReceipt(sale.Id,true),ShowTransactionHistory);else _pos.RefreshCatalog();return _pos;}
 internal void SmokeSignIn(bool pin=false){ShowLogin();if(pin){Button? Find(DependencyObject root){if(root is Button button&&button.Content?.ToString()=="PIN")return button;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var match=Find(VisualTreeHelper.GetChild(root,i));if(match!=null)return match;}return null;}UpdateLayout();var mode=Find((DependencyObject)Content)??throw new InvalidOperationException("PIN login option is missing.");mode.Command.Execute(null);}}
 internal void SmokeHistory()=>ShowTransactionHistory();
 internal void SmokeManagementDialogs()=>ManagementPageFactory.SmokeDialogs(_business,_user!,_auth);
 internal void SmokeCalendarView(string mode)=>ManagementPageFactory.SelectCalendarView((FrameworkElement)_page.Content,mode);
 internal void SmokePinDialog()=>ShowPinSettings();
 internal void SmokeProfileDialog()=>ShowProfileSettings();
 internal void SmokeAccountDialogs()=>ManagementPageFactory.SmokeAccountDialogs(_business,_user!,_auth);
 internal void SmokeFullScreen()=>ToggleFullScreen();
 internal void SmokeSettleAnimations(){_page.BeginAnimation(OpacityProperty,null);_page.Opacity=1;_page.RenderTransform=Transform.Identity;if(_page.Content is FrameworkElement view && (view.ActualWidth<=0||view.ActualHeight<=0))throw new InvalidOperationException("The active page has no usable layout.");}
 internal void SmokePosDialogs(){var line=_pos!.SmokeDialogs();var sale=_business.Checkout(new CheckoutRequest{Lines=[line],Payments=[new(){Method=PaymentMethod.Cash,Amount=1100}],CheckoutToken=Guid.NewGuid()},_user!);ShowReceipt(sale.Id,true);}
}


