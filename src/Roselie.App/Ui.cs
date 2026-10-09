using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Roselie.App;

public abstract class Observable : INotifyPropertyChanged
{
 public event PropertyChangedEventHandler? PropertyChanged;
 protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name=null) { if (EqualityComparer<T>.Default.Equals(field,value)) return false; field=value; Raise(name); return true; }
 protected void Raise([CallerMemberName] string? name=null) => PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
}

public sealed class RelayCommand(Action execute, Func<bool>? canExecute=null) : ICommand
{
 public bool CanExecute(object? parameter)=>canExecute?.Invoke()??true;
 public void Execute(object? parameter)=>execute();
 public event EventHandler? CanExecuteChanged { add=>CommandManager.RequerySuggested+=value; remove=>CommandManager.RequerySuggested-=value; }
}

public static class Ui
{
 public static bool ReduceMotion { get; set; }
 public static bool MotionEnabled => SystemParameters.ClientAreaAnimation && !ReduceMotion;
 public static void Enter(FrameworkElement element)
 {
  element.BeginAnimation(UIElement.OpacityProperty, null);
  element.Opacity = 1;
  if (!MotionEnabled) return;
  var move = new TranslateTransform(); element.RenderTransform = move;
  element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
  move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(7, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
 }
 public static void Fade(FrameworkElement element, double from, double to, Action? completed = null)
 {
  if (!MotionEnabled) { element.Opacity = to; completed?.Invoke(); return; }
  var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(180));
  if (completed != null) animation.Completed += (_, _) => completed();
  element.BeginAnimation(UIElement.OpacityProperty, animation);
 }
 public static TextBlock Icon(string glyph, double size = 18) => new() { Text = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = size, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
 public static Button IconButton(string glyph, string tooltip, Action action)
 {
  var b = Button("", action); b.Content = Icon(glyph); b.ToolTip = tooltip;
  b.Style = (Style)Application.Current.FindResource("IconButton");
  System.Windows.Automation.AutomationProperties.SetName(b, tooltip); return b;
 }
 public static FrameworkElement SignOutIcon()
 {
  var icon=new System.Windows.Shapes.Path{Data=Geometry.Parse("M 9,3 L 3,3 L 3,17 L 9,17 M 9,10 L 18,10 M 14,6 L 18,10 L 14,14"),StrokeThickness=1.7,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,Width=20,Height=20,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
  icon.SetBinding(System.Windows.Shapes.Shape.StrokeProperty,new Binding("Foreground"){RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(Button),1)});return icon;
 }
 internal static Action<Window>? SmokeDialogObserver { get; set; }
 public static event Action<string, string>? OnToast;

 public static void Toast(string message, string type = "success") => OnToast?.Invoke(message, type);

 public static Brush Brush(string hex)=>(Brush)new BrushConverter().ConvertFromString(hex)!;

 public static TextBlock Text(string value,double size=14,string color="#344052",bool bold=false)=>new(){Text=value,FontSize=size,Foreground=Brush(color),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap};

 public static Button Button(string label,Action action,bool primary=false) { var b=new Button{Content=label,Margin=new Thickness(0,0,8,0),Style=(Style)Application.Current.FindResource(primary?"PrimaryButton":"SecondaryButton")}; b.Command=new RelayCommand(()=>Guard(action)); var scale=new ScaleTransform(1,1);b.RenderTransform=scale;b.RenderTransformOrigin=new Point(.5,.5);void Size(double v){if(!MotionEnabled)return;scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(v,TimeSpan.FromMilliseconds(150)));scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(v,TimeSpan.FromMilliseconds(150)));}b.PreviewMouseLeftButtonDown+=(_,_)=>Size(.98);b.PreviewMouseLeftButtonUp+=(_,_)=>Size(1);b.MouseLeave+=(_,_)=>Size(1); return b; }

 public static Button DangerButton(string label, Action action)
 {
  var b = new Button { Content = label, Margin = new Thickness(0, 0, 8, 0), Style = (Style)Application.Current.FindResource("DangerButton") };
  b.Command = new RelayCommand(() => Guard(action));
  return b;
 }

 public static Button WarningButton(string label, Action action)
 {
  var b = new Button { Content = label, Margin = new Thickness(0, 0, 8, 0), Style = (Style)Application.Current.FindResource("WarningButton") };
  b.Command = new RelayCommand(() => Guard(action));
  return b;
 }

 public static Button Pill(string label,Action action,bool active=false)
 {
  var b=new Button{Content=label,Margin=new Thickness(0,0,8,8),Style=(Style)Application.Current.FindResource(active?"ActivePillButton":"PillButton")};
  b.Command=new RelayCommand(()=>Guard(action));
  return b;
 }

 public static void Guard(Action action) { try{action();}catch(Exception ex){var body=Text(ex.GetBaseException().Message,14,"#77727B");var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};var dialog=Dialog("Please check the details",body,510,300,actions);var close=Button("Got it",()=>dialog.Close(),true);close.IsDefault=true;close.Margin=new Thickness(0);actions.Children.Add(close);dialog.ShowDialog();} }

 public static Border Card(UIElement content,Thickness? margin=null)=>new(){Child=content,Style=(Style)Application.Current.FindResource("Card"),Margin=margin??new Thickness(0,0,0,16)};

 public static Border InteractiveCard(UIElement content,Thickness? margin=null)
 {
  var card=new Border{Child=content,Style=(Style)Application.Current.FindResource("InteractiveCard"),Margin=margin??new Thickness(0,0,0,16)};var move=new TranslateTransform();card.RenderTransform=move;
  void Lift(double target){if(!MotionEnabled)return;move.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(target,TimeSpan.FromMilliseconds(180)){EasingFunction=new QuadraticEase{EasingMode=EasingMode.EaseOut}});}
  card.MouseEnter+=(_,_)=>Lift(-2);card.MouseLeave+=(_,_)=>Lift(0);return card;
 }

 public static Border Badge(string text,string backgroundHex="#F3E7E7",string foregroundHex="#A85E67",double size=11,bool bold=true)=>new()
 {
  Background=Brush(backgroundHex),CornerRadius=new CornerRadius(12),Padding=new Thickness(10,3,10,3),Child=Text(text,size,foregroundHex,bold),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Left
 };

 public static Border StatusPill(string status)
 {
  var s = status.Trim();
  var (bg, fg) = s.ToLowerInvariant() switch
  {
   "active" or "confirmed" or "completed" or "paid" or "available" => ("#EAF5F0", "#27845F"),
   "pending" or "inprogress" or "in progress" or "starts at" or "range" or "expiring soon" or "accrued" => ("#FEF3C7", "#C88B35"),
   "cancelled" or "inactive" or "void" or "voided" or "refunded" or "expired" => ("#FEE2E2", "#C84D58"),
   "partiallyrefunded" or "partially refunded" => ("#FEF3C7", "#C88B35"),
   "low stock" or "review required" => ("#FEF2F2", "#C84D58"),
   _ => ("#F3F4F6", "#77727B")
  };
  return Badge(s, bg, fg, 11, true);
 }

 public static string? CustomLogoPath { get; set; }
 public static event Action? OnLogoChanged;
 public static void NotifyLogoChanged() => OnLogoChanged?.Invoke();

 public static Image CreateLoginHeroImage()
 {
  var img = new Image { Stretch = Stretch.UniformToFill };
  try
  {
   var biRes = new BitmapImage();
   biRes.BeginInit();
   biRes.CacheOption = BitmapCacheOption.OnLoad;
   biRes.UriSource = new Uri("pack://application:,,,/Roselie.POS;component/Assets/login-hero.jpg", UriKind.Absolute);
   biRes.EndInit();
   img.Source = biRes;
   return img;
  }
  catch
  {
   try
   {
    var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "login-hero.jpg");
    if (File.Exists(fallback))
    {
     var bi = new BitmapImage();
     bi.BeginInit();
     bi.CacheOption = BitmapCacheOption.OnLoad;
     bi.UriSource = new Uri(fallback, UriKind.Absolute);
     bi.EndInit();
     img.Source = bi;
     return img;
    }
   }
   catch { }
  }
  return img;
 }

 public static Image CreateLogoImage(double width = 80, double height = 80)
 {
  var img = new Image { Stretch = Stretch.Uniform };
  if (!double.IsNaN(width) && width > 0) img.Width = width;
  if (!double.IsNaN(height) && height > 0) img.Height = height;
  try
  {
   if (!string.IsNullOrEmpty(CustomLogoPath) && File.Exists(CustomLogoPath))
   {
    var bi = new BitmapImage();
    bi.BeginInit();
    bi.CacheOption = BitmapCacheOption.OnLoad;
    bi.UriSource = new Uri(CustomLogoPath, UriKind.Absolute);
    bi.EndInit();
    img.Source = bi;
    return img;
   }
   var biRes = new BitmapImage();
   biRes.BeginInit();
   biRes.CacheOption = BitmapCacheOption.OnLoad;
   biRes.UriSource = new Uri("pack://application:,,,/Roselie.POS;component/Assets/logo.png", UriKind.Absolute);
   biRes.EndInit();
   img.Source = biRes;
  }
  catch
  {
   try
   {
    var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
    if (File.Exists(fallback))
    {
     var bi = new BitmapImage();
     bi.BeginInit();
     bi.CacheOption = BitmapCacheOption.OnLoad;
     bi.UriSource = new Uri(fallback, UriKind.Absolute);
     bi.EndInit();
     img.Source = bi;
    }
   }
   catch { }
  }
  return img;
 }

 public static StackPanel Stack(params UIElement[] children) { var s=new StackPanel();foreach(var c in children)s.Children.Add(c);return s; }

 public static StackPanel Row(params UIElement[] children) { var s=Stack(children);s.Orientation=Orientation.Horizontal;return s; }

 public static FrameworkElement Field(string label,FrameworkElement input) { input.Margin=new Thickness(0,6,0,14); return Stack(Text(label,13,"#77727B"),input); }

 public static TextBox Input(string initial="",double width=double.NaN)=>new(){Text=initial,Width=width,HorizontalAlignment=double.IsNaN(width)?HorizontalAlignment.Stretch:HorizontalAlignment.Left};
 public static TextBox SearchBox(string placeholder="Search…",double width=double.NaN)
 {
  var box=Input("",width);box.Tag=placeholder;box.Style=(Style)Application.Current.FindResource("SearchInput");System.Windows.Automation.AutomationProperties.SetName(box,placeholder);return box;
 }
 public static TextBox CurrencyInput(decimal amount,double width=140)=>NumericInput(amount.ToString("0.00",CultureInfo.CurrentCulture),width);
 public static TextBox PercentageInput(decimal amount,double width=100)=>NumericInput(amount.ToString("0.##",CultureInfo.CurrentCulture),width);
 private static TextBox NumericInput(string initial,double width){var box=Input(initial,width);box.Style=(Style)Application.Current.FindResource("NumericInput");return box;}

 public static decimal Number(TextBox field,string label) { if(!decimal.TryParse(field.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var result))throw new InvalidOperationException($"Enter a valid {label}.");return result; }

 public static Border EmptyState(string title, string description, string glyph = "\uE8A5")
 {
  var icon=Icon(glyph,28);icon.Foreground=Brush("#C8797D");icon.Margin=new Thickness(0,0,0,12);
  var heading=Text(title,16,"#344052",true);heading.TextAlignment=TextAlignment.Center;
  var note=Text(description,13,"#77727B");note.TextAlignment=TextAlignment.Center;note.MaxWidth=420;note.Margin=new Thickness(0,7,0,0);
  return new Border { Padding=new Thickness(24,30,24,30),Child=Stack(icon,heading,note),HorizontalAlignment=HorizontalAlignment.Stretch };
 }

 public static bool Confirm(string title,string message,string actionLabel="Confirm",bool destructive=false)
 {
  var accepted=false;var body=Text(message,14,"#77727B");var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
  var dialog=Dialog(title,body,510,300,actions);
  var cancel=Button("Cancel",()=>dialog.Close());cancel.IsCancel=true;
  var action=destructive?DangerButton(actionLabel,()=>{accepted=true;dialog.Close();}):Button(actionLabel,()=>{accepted=true;dialog.Close();},true);
  cancel.Margin=new Thickness(0,0,12,0);action.Margin=new Thickness(0);actions.Children.Add(cancel);actions.Children.Add(action);dialog.ShowDialog();return accepted;
 }

 public static string Money(decimal amount)=>amount.ToString("C2",CultureInfo.GetCultureInfo("en-PH"));

 public static DataGrid Table<T>(IEnumerable<T> rows, params (string Header,string Property)[] columns) { var g=new DataGrid{ItemsSource=rows,MinHeight=130}; foreach(var c in columns)g.Columns.Add(new DataGridTextColumn{Header=c.Header,Binding=new Binding(c.Property),Width=new DataGridLength(1,DataGridLengthUnitType.Star)}); return g; }

 public static ScrollViewer Scroll(UIElement child)=>new(){Content=child,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};

 public static Window Dialog(string title,UIElement content,double width=540,double height=640,UIElement? footer=null)
 {
  var owner=Application.Current.Windows.OfType<Window>().FirstOrDefault(w=>w.IsActive)??Application.Current.MainWindow;
  var area=SystemParameters.WorkArea;
  var surface=new Border{Padding=new Thickness(24),Child=content,Background=Brushes.White};
  var window=new Window{Title=title,Content=Scroll(surface),Owner=owner,Width=Math.Min(width,area.Width-40),Height=Math.Min(height,area.Height-48),MinWidth=Math.Min(320,area.Width-40),MaxWidth=area.Width-24,MaxHeight=area.Height-24,WindowStartupLocation=WindowStartupLocation.CenterOwner,Style=(Style)Application.Current.FindResource("ModalWindow"),Tag=CreateLogoImage(68,32).Source,Icon=owner?.Icon};
  window.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,(_,_)=>window.Close()));
  if(footer!=null){var scroll=(ScrollViewer)window.Content;window.Content=null;var root=new Grid();root.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.Children.Add(scroll);var actions=new Border{Child=footer,Style=(Style)Application.Current.FindResource("ModalFooter")};Grid.SetRow(actions,1);root.Children.Add(actions);window.Content=root;}
  window.PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;window.Close();}};
  DimAdorner? dim=null;AdornerLayer? layer=null;
  window.Loaded+=(_,_)=>{Enter(surface);if(window.Template.FindName("DialogHeader",window) is UIElement header)header.MouseLeftButtonDown+=(_,e)=>{if(e.Source is not System.Windows.Controls.Button && e.LeftButton==MouseButtonState.Pressed)window.DragMove();};if(owner?.Content is UIElement target){layer=AdornerLayer.GetAdornerLayer(target);if(layer!=null){dim=new DimAdorner(target);layer.Add(dim);}}window.Dispatcher.BeginInvoke(()=>{if(FindFocusable(content) is { } focus)Keyboard.Focus(focus);},DispatcherPriority.Input);};
  window.Closed+=(_,_)=>{if(dim!=null)layer?.Remove(dim);};
  if(SmokeDialogObserver!=null){window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-20000;window.Top=0;window.Loaded+=async (_,_)=>{await Task.Delay(240);try{SmokeDialogObserver?.Invoke(window);}finally{window.Close();}};}
  return window;
 }
 private static IInputElement? FindFocusable(DependencyObject root)
 {
  if(root is TextBox or PasswordBox or ComboBox && root is UIElement element && element.IsEnabled && element.IsVisible)return element;
  for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var match=FindFocusable(VisualTreeHelper.GetChild(root,i));if(match!=null)return match;}return null;
 }
 private sealed class DimAdorner(UIElement element):Adorner(element)
 {
  protected override void OnRender(DrawingContext drawingContext){IsHitTestVisible=false;drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(72,40,30,38)),null,new Rect(AdornedElement.RenderSize));}
 }
}

