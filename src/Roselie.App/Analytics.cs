using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Roselie.Core.Models;
using Roselie.Infrastructure;

namespace Roselie.App;

public sealed partial class MainWindow
{
 private FrameworkElement BuildDashboard()
 {
  var root=new DockPanel();
  var selected=SalonTime.Today;
  var from=new DatePicker{SelectedDate=new DateTime(selected.Year,selected.Month,1),Width=126};
  var to=new DatePicker{SelectedDate=selected,Width=126};
  var body=new StackPanel();
  var periodText=Ui.Text("",12,"#77727B");
  var hour=SalonTime.Now.Hour;
  var greeting=hour<12?"Good morning":hour<18?"Good afternoon":"Good evening";
  var header=new Grid{Margin=new Thickness(0,0,0,16)};
  header.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var displayName=string.IsNullOrWhiteSpace(_user?.DisplayName)?_user?.Username??"Administrator":_user.DisplayName;
  header.Children.Add(Ui.Stack(Ui.Text($"{greeting}, {displayName}",25,"#344052",true),Ui.Text("Here's what's happening at Roselie's Beauty Lounge.",13,"#77727B")));
  var date=Ui.Badge(selected.ToString("dddd, dd MMM yyyy"),"#FFFFFF","#77727B",12,false);
  date.Margin=new Thickness(14,0,0,0);Grid.SetColumn(date,1);header.Children.Add(date);
  body.Children.Add(header);
  var content=new StackPanel();body.Children.Add(content);
  Action<double>? reflow=null;
  content.SizeChanged+=(_,e)=>reflow?.Invoke(e.NewSize.Width);

  void Refresh()
  {
   var start=(from.SelectedDate??selected).Date;
   var end=(to.SelectedDate??selected).Date.AddDays(1);
   if(end<=start)throw new InvalidOperationException("The end date must be on or after the start date.");
   periodText.Text=$"{start:dd MMM} – {end.AddDays(-1):dd MMM yyyy} · Philippine time";
   var report=_business.GetReport(SalonTime.ToUtc(start),SalonTime.ToUtc(end),_user!);
   var monthStart=new DateTime(selected.Year,selected.Month,1);
   var month=_business.GetReport(SalonTime.ToUtc(monthStart),SalonTime.ToUtc(monthStart.AddMonths(1)),_user!);
   var today=_business.GetReport(SalonTime.ToUtc(selected),SalonTime.ToUtc(selected.AddDays(1)),_user!);
   var previousDay=_business.GetReport(SalonTime.ToUtc(selected.AddDays(-1)),SalonTime.ToUtc(selected),_user!);
   var previousMonth=_business.GetReport(SalonTime.ToUtc(monthStart.AddMonths(-1)),SalonTime.ToUtc(monthStart),_user!);
   var customers=_business.List<Customer>(_user!);
   var products=_business.List<Product>(_user!);
   var appointments=_business.List<Appointment>(_user!);
   content.Children.Clear();
   string Change(decimal current,decimal previous,string period)=>$"{(current-previous>=0?"+":"")}{Ui.Money(current-previous)} vs. {period}";
   var metrics=new UniformGrid{Columns=4};
   (string Label,string Value,string Note,string Glyph,string Color)[] cards=[
    ("Today's sales",Ui.Money(today.NetSales),previousDay.TransactionCount>0?Change(today.NetSales,previousDay.NetSales,"yesterday"):"Net revenue today","₱","#A85E67"),
    ("Monthly income",Ui.Money(month.NetSales),previousMonth.TransactionCount>0?Change(month.NetSales,previousMonth.NetSales,"last month"):selected.ToString("MMMM yyyy"),"↗","#A85E67"),
    ("Monthly net profit",Ui.Money(month.NetProfit),previousMonth.TransactionCount>0?Change(month.NetProfit,previousMonth.NetProfit,"last month"):"After recorded costs","◇",month.NetProfit>=0?"#27845F":"#C84D58"),
    ("Monthly expenses",Ui.Money(month.TotalExpenses),"Operating costs + commissions","▤","#77727B"),
    ("Transactions",report.TransactionCount.ToString("N0"),"Selected period","▣","#A85E67"),
    ("Customers",customers.Count.ToString("N0"),"Customer directory","♙","#77727B"),
    ("Today's appointments",appointments.Count(a=>SalonTime.FromUtc(a.StartsAtUtc).Date==selected&&a.Status!=AppointmentStatus.Cancelled).ToString("N0"),"Scheduled visits","◷","#A85E67"),
    ("Low stock alerts",products.Count(p=>p.IsActive&&p.StockQuantity<=p.ReorderLevel).ToString("N0"),"Products to replenish","!","#C88B35")
   ];
   foreach(var c in cards)
   {
    var top=new Grid();top.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});top.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
    var label=Ui.Text(c.Label,12,"#77727B",true);label.VerticalAlignment=VerticalAlignment.Center;top.Children.Add(label);
    var icon=new Border{Width=28,Height=28,Background=Ui.Brush("#FAF6F5"),CornerRadius=new CornerRadius(9),Child=new TextBlock{Text=c.Glyph,FontSize=17,Foreground=Ui.Brush(c.Color),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}};
    Grid.SetColumn(icon,1);top.Children.Add(icon);
    var value=Ui.Text(c.Value,24,c.Color=="#C84D58"?c.Color:"#344052",true);value.Margin=new Thickness(0,9,0,5);value.TextWrapping=TextWrapping.NoWrap;value.TextTrimming=TextTrimming.CharacterEllipsis;value.ToolTip=c.Value;
    var note=Ui.Text(c.Note,11,"#77727B");
    var card=Ui.Card(Ui.Stack(top,value,note),new Thickness(0,0,12,12));card.Padding=new Thickness(16);metrics.Children.Add(card);
   }
   content.Children.Add(metrics);
   if(report.IncompleteCostTransactions>0||month.IncompleteCostTransactions>0)
    content.Children.Add(DashboardNotice($"Profit needs a cost review · {month.IncompleteCostTransactions} record(s) this month and {report.IncompleteCostTransactions} in the selected period have incomplete costs.","#C88B35",()=>Navigate("Services"),"Review costs"));
   if(_business.List<SalonService>(_user!).Any(s=>!s.OwnerConfirmed))
    content.Children.Add(DashboardNotice("Your service catalog is ready for owner review. Confirm prices, durations and materials before business use.","#A85E67",()=>Navigate("Services"),"Review catalog"));

   var charts=new Grid();charts.ColumnDefinitions.Add(new(){Width=new GridLength(1.55,GridUnitType.Star)});charts.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});charts.RowDefinitions.Add(new(){Height=GridLength.Auto});charts.RowDefinitions.Add(new(){Height=GridLength.Auto});
   var salesChart=Ui.Card(Ui.Stack(Ui.Text("Sales performance",17,"#344052",true),Ui.Text("Daily net revenue · hover a bar for the exact amount",12,"#77727B"),DailySalesChart(report.DailySales)),new Thickness(0,0,12,14));charts.Children.Add(salesChart);
   var breakdown=Ui.Stack(Ui.Text("Income & profit",17,"#344052",true),Ui.Text("Selected period",12,"#77727B"),ValueRow("Net revenue",report.NetSales),ValueRow("Retail product costs",report.CostOfGoods),ValueRow("Service materials",report.MaterialCost),ValueRow("Staff commissions",report.CommissionCost),ValueRow("Operating expenses",report.OperatingExpenses),ValueRow("Net profit",report.NetProfit));
   var breakdownCard=Ui.Card(breakdown,new Thickness(0,0,0,14));Grid.SetColumn(breakdownCard,1);charts.Children.Add(breakdownCard);content.Children.Add(charts);

   var compare=Ui.Stack(Ui.Text("Monthly income vs. net profit",17,"#344052",true),Ui.Text(selected.ToString("MMMM yyyy"),12,"#77727B"));
   if(month.TransactionCount==0)compare.Children.Add(Ui.EmptyState("A clear picture of your month","Income and profit will appear as transactions are recorded.","\uE9D9"));
   else
   {
    var basis=Math.Max(Math.Abs(month.NetSales),Math.Abs(month.NetProfit));
    compare.Children.Add(ValueRow("Income",month.NetSales));compare.Children.Add(ProportionBar(month.NetSales,basis,"#C8797D"));
    compare.Children.Add(ValueRow("Net profit",month.NetProfit));compare.Children.Add(ProportionBar(month.NetProfit,basis,month.NetProfit>=0?"#27845F":"#C84D58"));
   }
   content.Children.Add(Ui.Card(compare));
   var ranks=new UniformGrid{Columns=3};
   ranks.Children.Add(RankedCard("Top services",report.TopServices,"No service sales yet","Completed service sales for this period will appear here."));
   ranks.Children.Add(RankedCard("Best-selling products",report.TopProducts,"No product sales yet","Retail sales for this period will appear here."));
   ranks.Children.Add(RankedCard("Expense breakdown",report.ExpenseBreakdown,"No expenses recorded","Recorded operating costs and commissions will appear here.","#C88B35"));
   content.Children.Add(ranks);

   var recent=_business.List<Sale>(_user!).OrderByDescending(s=>s.CompletedAtUtc).Take(8).Select(s=>new{Id=s.Id,s.TransactionNumber,Date=SalonTime.FromUtc(s.CompletedAtUtc).ToString("dd MMM · h:mm tt"),Total=Ui.Money(s.Total),Status=HistoryStatusLabel(s.Status)}).ToList();
   var recentHead=DashboardSectionHeader("Recent transactions","Transaction history",ShowTransactionHistory);
   var recentBody=Ui.Stack(recentHead);
   if(recent.Count==0)recentBody.Children.Add(Ui.EmptyState("Your first sale starts here","Completed transactions will appear here with their receipts.","\uE8A5"));
   else
   {
    var table=Ui.Table(recent,("Transaction","TransactionNumber"),("Date & time","Date"),("Total","Total"),("Status","Status"));table.Height=220;AlignNumberColumn(table.Columns[2]);
    table.MouseDoubleClick+=(_,_)=>{if(table.SelectedItem is{}item){var id=(Guid)item.GetType().GetProperty("Id")!.GetValue(item)!;Ui.Guard(()=>ShowReceipt(id));}};recentBody.Children.Add(table);
   }
   content.Children.Add(Ui.Card(recentBody));
   var names=customers.ToDictionary(c=>c.Id,c=>c.FullName);
   var upcoming=appointments.Where(a=>(a.Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed)&&a.StartsAtUtc>=DateTime.UtcNow).OrderBy(a=>a.StartsAtUtc).Take(5).ToList();
   var upcomingPanel=Ui.Stack(DashboardSectionHeader("Upcoming appointments","Calendar",()=>Navigate("Appointments")));
   if(upcoming.Count==0)upcomingPanel.Children.Add(Ui.EmptyState("No upcoming appointments","New confirmed and pending visits will appear here.","\uE787"));
   else foreach(var appointment in upcoming)upcomingPanel.Children.Add(ActivityRow(names.GetValueOrDefault(appointment.CustomerId,"Customer"),SalonTime.FromUtc(appointment.StartsAtUtc).ToString("dd MMM · h:mm tt"),Ui.StatusPill(appointment.Status.ToString())));
   var low=products.Where(p=>p.IsActive&&p.StockQuantity<=p.ReorderLevel).OrderBy(p=>p.StockQuantity-p.ReorderLevel).Take(5).ToList();
   var lowPanel=Ui.Stack(DashboardSectionHeader("Low stock alerts","Inventory",()=>Navigate("Inventory")));
   if(low.Count==0)lowPanel.Children.Add(Ui.EmptyState("Stock levels look good","Products at or below their reorder level will be flagged here.","\uE73E"));
   else foreach(var product in low)lowPanel.Children.Add(ActivityRow(product.Name,$"Reorder level {product.ReorderLevel:0.######} {product.StockUnit}",Ui.Badge($"{product.StockQuantity:0.######} {product.StockUnit}","#FFF5E8","#C88B35")));
   var activity=new UniformGrid{Columns=2};activity.Children.Add(Ui.Card(upcomingPanel,new Thickness(0,0,12,0)));activity.Children.Add(Ui.Card(lowPanel,new Thickness(0)));content.Children.Add(activity);
   void Reflow(double width)
   {
    metrics.Columns=width<800?2:4;ranks.Columns=width<820?1:3;activity.Columns=width<820?1:2;
    var compact=width<830;Grid.SetColumn(breakdownCard,compact?0:1);Grid.SetRow(breakdownCard,compact?1:0);
    salesChart.Margin=new Thickness(0,0,compact?0:12,14);charts.ColumnDefinitions[1].Width=compact?new GridLength(0):new GridLength(1,GridUnitType.Star);
   }
   reflow=Reflow;Reflow(body.ActualWidth);
  }
  var toolbar=new WrapPanel{VerticalAlignment=VerticalAlignment.Center};
  toolbar.Children.Add(CompactFilter("From",from));toolbar.Children.Add(CompactFilter("To",to));
  toolbar.Children.Add(Ui.Button("Today",()=>{from.SelectedDate=selected;to.SelectedDate=selected;Refresh();}));
  toolbar.Children.Add(Ui.Button("This week",()=>{from.SelectedDate=selected.AddDays(-((int)selected.DayOfWeek+6)%7);to.SelectedDate=selected;Refresh();}));
  toolbar.Children.Add(Ui.Button("This month",()=>{from.SelectedDate=new DateTime(selected.Year,selected.Month,1);to.SelectedDate=selected;Refresh();}));
  toolbar.Children.Add(Ui.Button("Apply",Refresh,true));
  var newSale=Ui.Button("+ New sale",()=>Navigate("Point of Sale"),true);toolbar.Children.Add(newSale);
  foreach(var button in toolbar.Children.OfType<Button>())
  {
   button.Height=40;button.VerticalAlignment=VerticalAlignment.Bottom;button.Margin=new Thickness(0,0,8,8);
  }
  var toolbarCard=Ui.Card(Ui.Stack(toolbar,periodText),new Thickness(0,0,0,16));toolbarCard.Padding=new Thickness(14,12,14,12);periodText.Margin=new Thickness(0,8,0,0);
  DockPanel.SetDock(toolbarCard,Dock.Top);root.Children.Add(toolbarCard);root.Children.Add(Ui.Scroll(body));Refresh();return root;
 }

 private static FrameworkElement CompactFilter(string label,FrameworkElement input)
 {
  var text=Ui.Text(label,11,"#77727B",true);text.Margin=new Thickness(0,0,0,5);
  var field=Ui.Stack(text,input);field.Margin=new Thickness(0,0,10,8);return field;
 }
 private static Border DashboardNotice(string message,string color,Action action,string actionLabel)
 {
  var row=new Grid();row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var text=Ui.Text(message,12,color);text.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(text);
  var button=Ui.Button(actionLabel,action);button.Margin=new Thickness(16,0,0,0);Grid.SetColumn(button,1);row.Children.Add(button);
  var card=Ui.Card(row,new Thickness(0,0,0,12));card.Padding=new Thickness(14,10,14,10);card.BorderBrush=Ui.Brush(color);return card;
 }
 private static FrameworkElement DashboardSectionHeader(string title,string actionLabel,Action action)
 {
  var row=new Grid{Margin=new Thickness(0,0,0,10)};row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  row.Children.Add(Ui.Text(title,17,"#344052",true));var button=Ui.Button(actionLabel,action);button.Margin=new Thickness(12,0,0,0);Grid.SetColumn(button,1);row.Children.Add(button);return row;
 }
 private static FrameworkElement ActivityRow(string title,string subtitle,UIElement trailing)
 {
  var row=new Grid{Margin=new Thickness(0,8,0,8)};row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var text=Ui.Stack(Ui.Text(title,13,"#344052",true),Ui.Text(subtitle,11,"#77727B"));text.Margin=new Thickness(0,0,12,0);row.Children.Add(text);Grid.SetColumn(trailing,1);row.Children.Add(trailing);return row;
 }
 private static FrameworkElement ProportionBar(decimal value,decimal basis,string color)
 {
  var fraction=basis==0?0:Math.Min(1,(double)(Math.Abs(value)/basis));
  var track=new Grid{Height=8,Margin=new Thickness(0,8,0,4),Background=Ui.Brush("#F3E7E7")};track.ColumnDefinitions.Add(new(){Width=new GridLength(fraction,GridUnitType.Star)});track.ColumnDefinitions.Add(new(){Width=new GridLength(1-fraction,GridUnitType.Star)});
  track.Children.Add(new Border{Background=Ui.Brush(color),CornerRadius=new CornerRadius(4)});return track;
 }
 private static Border RankedCard(string title,IReadOnlyList<ReportValue> values,string emptyTitle,string emptyDescription,string color="#C8797D")
 {
  var panel=Ui.Stack(Ui.Text(title,17,"#344052",true));
  if(values.Count==0)panel.Children.Add(Ui.EmptyState(emptyTitle,emptyDescription,"\uE9D9"));
  else
  {
   var basis=values.Select(v=>Math.Abs(v.Amount)).DefaultIfEmpty(0).Max();
   foreach(var value in values.Take(5)){panel.Children.Add(ValueRow(value.Label,value.Amount));panel.Children.Add(ProportionBar(value.Amount,basis,value.Amount<0?"#C84D58":color));}
  }
  return Ui.Card(panel,new Thickness(0,0,12,16));
 }
 private static FrameworkElement DailySalesChart(IReadOnlyList<ReportValue> values)
 {
  if(values.Count==0)return Ui.EmptyState("Your sales story starts here","Complete a transaction to see daily revenue and trends.","\uE9D9");
  var points=values.TakeLast(31).ToArray();
  var canvas=new Canvas{Height=172,ClipToBounds=true,Margin=new Thickness(0,18,0,8)};
  var hint=Ui.Text(points.Length==values.Count?"Net sales include refunds and discounts.":"Showing the latest 31 recorded days in this period.",11,"#77727B");
  void Draw()
  {
   canvas.Children.Clear();var width=canvas.ActualWidth;if(width<30)return;
   var positive=points.Select(p=>Math.Max(0,p.Amount)).Max();var negative=points.Select(p=>Math.Max(0,-p.Amount)).Max();
   var range=positive+negative;var scale=range==0?0:140/(double)range;var baseline=range==0?150:12+(double)positive*scale;
   var axis=new Border{Width=width,Height=1,Background=Ui.Brush("#E9DADB")};Canvas.SetTop(axis,baseline);canvas.Children.Add(axis);
   var slot=width/points.Length;
   for(var i=0;i<points.Length;i++)
   {
    var value=points[i];var height=Math.Max(2,(double)Math.Abs(value.Amount)*scale);
    var bar=new Border{Width=Math.Max(2,Math.Min(30,slot*.65)),Height=height,Background=Ui.Brush(value.Amount<0?"#C84D58":"#C8797D"),CornerRadius=new CornerRadius(3),ToolTip=$"{value.Label}\nNet revenue {Ui.Money(value.Amount)}",Cursor=Cursors.Hand};
    Canvas.SetLeft(bar,i*slot+(slot-bar.Width)/2);Canvas.SetTop(bar,value.Amount<0?baseline:baseline-height);canvas.Children.Add(bar);
    bar.MouseEnter+=(_,_)=>{bar.Opacity=.75;hint.Text=$"{value.Label} · {Ui.Money(value.Amount)}";};
    bar.MouseLeave+=(_,_)=>{bar.Opacity=1;hint.Text=points.Length==values.Count?"Net sales include refunds and discounts.":"Showing the latest 31 recorded days in this period.";};
   }
  }
  canvas.SizeChanged+=(_,_)=>Draw();canvas.Loaded+=(_,_)=>Draw();
  var dates=new Grid();dates.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});dates.ColumnDefinitions.Add(new(){Width=GridLength.Auto});dates.Children.Add(Ui.Text(points[0].Label,10,"#77727B"));var last=Ui.Text(points[^1].Label,10,"#77727B");Grid.SetColumn(last,1);dates.Children.Add(last);
  return Ui.Stack(canvas,dates,hint);
 }
 private static FrameworkElement ValueRow(string label,decimal value)
 {
  var row=new Grid{Margin=new Thickness(0,10,0,0)};row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var text=Ui.Text(label,12,"#77727B");text.Margin=new Thickness(0,0,12,0);row.Children.Add(text);
  var amount=Ui.Text(Ui.Money(value),13,value<0?"#C84D58":"#344052",true);amount.TextWrapping=TextWrapping.NoWrap;Grid.SetColumn(amount,1);row.Children.Add(amount);return row;
 }
 private static void AlignNumberColumn(DataGridColumn column)
 {
  if(column is not DataGridTextColumn text)return;
  var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty,TextAlignment.Right));style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center));style.Setters.Add(new Setter(TextBlock.FontWeightProperty,FontWeights.SemiBold));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.NoWrap));style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty,text.Binding));text.ElementStyle=style;
  var header=new Style(typeof(DataGridColumnHeader),Application.Current.TryFindResource(typeof(DataGridColumnHeader)) as Style);header.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Right));text.HeaderStyle=header;
 }

 private FrameworkElement BuildReports()
 {
  string[] names=["Daily sales","Weekly sales","Monthly income","Monthly net profit","Monthly expenses","Service sales","Product sales","Inventory movements","Low stock","Employee performance","Employee commissions","Discounts","Refunds and voids","Payment methods","Package purchases and redemptions"];
  var root=new DockPanel();var picker=new ComboBox{ItemsSource=names,SelectedIndex=0,Width=242};
  var from=new DatePicker{SelectedDate=new DateTime(SalonTime.Today.Year,SalonTime.Today.Month,1),Width=126};var to=new DatePicker{SelectedDate=SalonTime.Today,Width=126};
  var view=new ContentControl();ReportData? current=null;var subtitle=Ui.Text("",12,"#77727B");var state=Ui.Text("",11,"#77727B");
  void Refresh()
  {
   var start=SalonTime.ToUtc((from.SelectedDate??SalonTime.Today).Date);var end=SalonTime.ToUtc((to.SelectedDate??SalonTime.Today).Date.AddDays(1));
   if(end<=start)throw new InvalidOperationException("Select a valid date range.");
   current=CreateReport((string)picker.SelectedItem,start,end);subtitle.Text=current.Title+" · "+current.Period;state.Text=$"{current.Rows.Count:N0} record(s) · All amounts in PHP · Philippine time";
   var data=new DataTable();for(var i=0;i<current.Columns.Length;i++)data.Columns.Add("C"+i,typeof(object));foreach(var row in current.Rows)data.Rows.Add(row.Select(v=>v??DBNull.Value).ToArray());
   var table=new DataGrid{AutoGenerateColumns=false,ItemsSource=data.DefaultView,SelectionMode=DataGridSelectionMode.Single,MinHeight=160,BorderThickness=new Thickness(0),Background=Brushes.White,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,EnableRowVirtualization=true,EnableColumnVirtualization=true};
   for(var i=0;i<current.Columns.Length;i++)
   {
    var index=i;var heading=current.Columns[i];var currency=heading.Contains("PHP",StringComparison.OrdinalIgnoreCase);
    var numeric=currency||current.Rows.Any(r=>index<r.Length&&r[index] is decimal or int or long);
    var unitCost=heading.Contains("unit cost",StringComparison.OrdinalIgnoreCase)||heading.Contains("average cost",StringComparison.OrdinalIgnoreCase);
    var label=new TextBlock{Text=heading,TextWrapping=TextWrapping.Wrap,TextAlignment=numeric?TextAlignment.Right:TextAlignment.Left,ToolTip=heading};
    var column=new DataGridTextColumn{Header=label,Binding=new Binding("[C"+i+"]"){Converter=new ReportCellConverter(currency,unitCost)},Width=new DataGridLength(numeric?1:1.6,DataGridLengthUnitType.Star),MinWidth=numeric?126:140};
    if(numeric)AlignNumberColumn(column);table.Columns.Add(column);
   }
   var container=new Grid();container.RowDefinitions.Add(new(){Height=GridLength.Auto});container.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});container.RowDefinitions.Add(new(){Height=GridLength.Auto});
   var headingPanel=Ui.Stack(Ui.Text(current.Title,18,"#344052",true),Ui.Text(current.Period,12,"#77727B"));headingPanel.Margin=new Thickness(18,16,18,14);container.Children.Add(headingPanel);
   if(current.Rows.Count==0){var empty=Ui.EmptyState("No records in this period","Choose another date range or record activity to populate this report.","\uE9D9");Grid.SetRow(empty,1);container.Children.Add(empty);}else{Grid.SetRow(table,1);container.Children.Add(table);}
   var count=Ui.Text($"{current.Rows.Count:N0} record(s) · Export includes the complete report",11,"#77727B");count.Margin=new Thickness(18,12,18,14);Grid.SetRow(count,2);container.Children.Add(count);
   var card=Ui.Card(container,new Thickness(0));card.Padding=new Thickness(0);view.Content=card;
  }
  void Export(string extension)
  {
   if(current==null)return;
   var save=new SaveFileDialog{FileName="Roselie-"+current.Title.Replace(' ','-')+"-"+SalonTime.Today.ToString("yyyyMMdd"),Filter=$"{extension.ToUpperInvariant()} file|*.{extension}",DefaultExt=extension};
   if(save.ShowDialog()==true){ReportExporter.Export(current,save.FileName);Ui.Toast($"Exported {current.Title} to {extension.ToUpperInvariant()}");}
  }
  var controls=new WrapPanel();controls.Children.Add(CompactFilter("Report",picker));controls.Children.Add(CompactFilter("From",from));controls.Children.Add(CompactFilter("To",to));
  var apply=Ui.Button("Apply filters",Refresh,true);apply.VerticalAlignment=VerticalAlignment.Bottom;apply.Margin=new Thickness(0,18,8,8);controls.Children.Add(apply);
  var year=Ui.Button("This year",()=>{from.SelectedDate=new DateTime(SalonTime.Today.Year,1,1);to.SelectedDate=SalonTime.Today;Refresh();});year.Margin=new Thickness(0,18,0,8);controls.Children.Add(year);
  var actions=new WrapPanel{Margin=new Thickness(0,10,0,0)};actions.Children.Add(Ui.Button("Preview / print",()=>{if(current!=null)PrintReport(current);}));actions.Children.Add(Ui.Button("Export PDF",()=>Export("pdf")));actions.Children.Add(Ui.Button("Export Excel",()=>Export("xlsx")));actions.Children.Add(Ui.Button("Export CSV",()=>Export("csv")));actions.Children.Add(Ui.Button("Transaction history",ShowTransactionHistory));
  var toolbar=Ui.Card(Ui.Stack(controls,actions),new Thickness(0,12,0,14));toolbar.Padding=new Thickness(14);
  var head=Ui.Stack(Ui.Text("Reporting center",24,"#344052",true),Ui.Text("Explore your business, then export or print the full report.",13,"#77727B"),toolbar,subtitle,state);head.Margin=new Thickness(0,0,0,12);
  DockPanel.SetDock(head,Dock.Top);root.Children.Add(head);root.Children.Add(view);picker.SelectionChanged+=(_,_)=>Ui.Guard(Refresh);Refresh();return root;
 }
 private sealed class ReportCellConverter(bool currency,bool unitCost):IValueConverter
 {
  public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value switch{null or DBNull=>"—",decimal d=>currency?(unitCost?"₱"+d.ToString("#,##0.00####",CultureInfo.GetCultureInfo("en-PH")):Ui.Money(d)):d.ToString("0.######",culture),DateTime date=>date.ToString("dd MMM yyyy · h:mm tt",culture),bool flag=>flag?"Yes":"No",_=>value.ToString()??""};
  public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
 }
 private ReportData CreateReport(string name, DateTime start, DateTime end)
 {
  var report = _business.GetReport(start, end, _user!);
  var sales = _business.List<Sale>(_user!);
  var selected = sales.Where(s => s.CompletedAtUtc >= start && s.CompletedAtUtc < end).ToList();
  var ids = selected.Select(s => s.Id).ToHashSet();
  var saleItems = _business.List<SaleItem>(_user!).Where(s => ids.Contains(s.SaleId)).ToList();
  ReportData Data(string[] columns, IEnumerable<object?[]> rows) => new(name, columns, rows.ToList(), $"{SalonTime.FromUtc(start):dd MMM yyyy} to {SalonTime.FromUtc(end).AddDays(-1):dd MMM yyyy}");

  if (name == "Daily sales") return Data(["Date", "Net sales (PHP)"], report.DailySales.Select(v => new object?[] { v.Label, v.Amount }));
  if (name == "Weekly sales") return Data(["Week starting Monday", "Net sales (PHP)"], report.DailySales.GroupBy(v => { var day = DateTime.ParseExact(v.Label, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); return day.AddDays(-((int)day.DayOfWeek + 6) % 7); }).OrderBy(g => g.Key).Select(g => new object?[] { g.Key.ToString("yyyy-MM-dd"), g.Sum(v => v.Amount) }));
  if (name is "Monthly income" or "Monthly net profit") return Data(["Metric", "Amount (PHP)"], new object?[][] { ["Gross sales", report.GrossSales], ["Discounts", report.Discounts], ["Refunds", report.Refunds], ["Net sales revenue", report.NetSales], ["Retail COGS", report.CostOfGoods], ["Service material costs", report.MaterialCost], ["Employee commissions", report.CommissionCost], ["Operating expenses", report.OperatingExpenses], ["Net profit", report.NetProfit], ["Net cash collection", report.CashCollections], ["Incomplete cost records", report.IncompleteCostTransactions] });
  if (name == "Monthly expenses") return Data(["Category", "Amount (PHP)"], report.ExpenseBreakdown.Select(v => new object?[] { v.Label, v.Amount }).Append(new object?[] { "Total expenses (accrual policy)", report.TotalExpenses }));
  if (name is "Service sales" or "Product sales") return Data(["Name", "Net sales (PHP)"], (name == "Service sales" ? report.TopServices : report.TopProducts).Select(v => new object?[] { v.Label, v.Amount }));
  var products = _business.List<Product>(_user!).ToDictionary(p => p.Id, p => p.Name);
  var employees = _business.List<Employee>(_user!).ToDictionary(e => e.Id, e => e.FullName);
  if (name == "Inventory movements") return Data(["Date", "Product", "Movement", "Quantity", "Balance", "Unit cost (PHP)", "Reason"], _business.List<InventoryMovement>(_user!).Where(m => m.OccurredAtUtc >= start && m.OccurredAtUtc < end).Select(m => new object?[] { SalonTime.FromUtc(m.OccurredAtUtc), products.GetValueOrDefault(m.ProductId), m.Type, m.Quantity, m.BalanceAfter, m.UnitCost, m.Reason }));
  if (name == "Low stock") return Data(["Product", "Stock", "Reorder level", "Unit", "Average cost (PHP)"], _business.List<Product>(_user!).Where(p => p.IsActive && p.StockQuantity <= p.ReorderLevel).Select(p => new object?[] { p.Name, p.StockQuantity, p.ReorderLevel, p.StockUnit, p.AverageCost }));
  if (name == "Employee performance")
  {
   var visits = saleItems.Where(i => i.EmployeeId != null && i.Kind is SaleItemKind.Service or SaleItemKind.PackageRedemption).Select(i => (EmployeeId: i.EmployeeId!.Value, Quantity: i.Quantity, Revenue: i.Total, Commission: i.CommissionCost)).ToList();
   visits.AddRange(_business.List<PackageRedemption>(_user!).Where(r => r.RedeemedAtUtc >= start && r.RedeemedAtUtc < end && r.SaleItemId == null && r.EmployeeId != null).Select(r => (r.EmployeeId!.Value, (decimal)r.SessionsUsed, 0m, r.CommissionCost)));
   return Data(["Employee", "Completed service quantity", "Net service sales (PHP)", "Commissions (PHP)"], visits.GroupBy(v => v.EmployeeId).Select(g => new object?[] { employees.GetValueOrDefault(g.Key), g.Sum(v => v.Quantity), g.Sum(v => v.Revenue), g.Sum(v => v.Commission) }));
  }
  if (name == "Employee commissions") return Data(["Date", "Employee", "Type", "Rate", "Amount (PHP)", "Paid", "Reversed"], _business.List<CommissionRecord>(_user!).Where(c => c.EarnedAtUtc >= start && c.EarnedAtUtc < end).Select(c => new object?[] { SalonTime.FromUtc(c.EarnedAtUtc), employees.GetValueOrDefault(c.EmployeeId), c.Type, c.Rate, c.Amount, SalonTime.FromUtc(c.PaidAtUtc), c.IsReversed }));
  if (name == "Discounts") return Data(["Transaction", "Item", "Item %", "Discount (PHP)", "Transaction allocation (PHP)"], saleItems.Where(i => i.DiscountAmount + i.OrderDiscountAmount > 0).Select(i => new object?[] { selected.First(s => s.Id == i.SaleId).TransactionNumber, i.Name, i.DiscountPercent, i.DiscountAmount, i.OrderDiscountAmount }));
  if (name == "Refunds and voids") return Data(["Date", "Refund number", "Type", "Amount (PHP)", "Payment", "Reason"], _business.List<Refund>(_user!).Where(r => r.RefundedAtUtc >= start && r.RefundedAtUtc < end).Select(r => new object?[] { SalonTime.FromUtc(r.RefundedAtUtc), r.RefundNumber, r.IsVoid ? "Void" : "Refund", r.Amount, r.Method, r.Reason }));
  if (name == "Payment methods") return Data(["Method", "Collected (PHP)", "Applied (PHP)"], _business.List<Payment>(_user!).Where(p => ids.Contains(p.SaleId)).GroupBy(p => p.Method).Select(g => new object?[] { g.Key, g.Sum(p => p.Amount), g.Sum(p => p.AppliedAmount) }));
  var packages = _business.List<CustomerPackage>(_user!);
  var customers = _business.List<Customer>(_user!).ToDictionary(c => c.Id, c => c.FullName);
  var packageRows = packages.Where(p => p.PurchasedAtUtc >= start && p.PurchasedAtUtc < end).Select(p => new object?[] { SalonTime.FromUtc(p.PurchasedAtUtc), "Purchase", customers.GetValueOrDefault(p.CustomerId), p.Name, p.PurchasePrice, p.TotalSessions, p.UsedSessions, p.RemainingSessions }).ToList();
  foreach (var r in _business.List<PackageRedemption>(_user!).Where(r => r.RedeemedAtUtc >= start && r.RedeemedAtUtc < end))
  {
   var p = packages.First(x => x.Id == r.CustomerPackageId);
   packageRows.Add([SalonTime.FromUtc(r.RedeemedAtUtc), "Redemption", customers.GetValueOrDefault(p.CustomerId), p.Name, 0m, p.TotalSessions, p.UsedSessions, p.RemainingSessions]);
  }
  return Data(["Date", "Event", "Customer", "Package", "Revenue (PHP)", "Sessions", "Used", "Remaining"], packageRows);
 }
}
