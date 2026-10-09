using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Roselie.Core.Models;

namespace Roselie.App;

public sealed partial class MainWindow
{
 private sealed record SaleRow(Sale Sale,string CustomerName,string Method,ICommand OpenCommand)
 {
  public string Transaction=>Sale.TransactionNumber;
  public string Date=>SalonTime.FromUtc(Sale.CompletedAtUtc).ToString("dd MMM yy · h:mm tt");
  public decimal Total=>Sale.Total;
  public SaleStatus Status=>Sale.Status;
 }
 private static string HistoryStatusLabel(SaleStatus status)=>status switch{SaleStatus.PartiallyRefunded=>"Partially refunded",_=>status.ToString()};
 private static string HistoryPaymentLabel(PaymentMethod method)=>method==PaymentMethod.BankTransfer?"Bank transfer":method==PaymentMethod.Card?"Card":""+method;

 private void ShowTransactionHistory()
 {
  var start=new DatePicker{SelectedDate=SalonTime.Today.AddDays(-30),Width=126};
  var end=new DatePicker{SelectedDate=SalonTime.Today,Width=126};
  var search=Ui.Input("",218);search.ToolTip="Search transaction number or customer name";
  var status=new ComboBox{ItemsSource=new[]{"All statuses","Completed","Partially refunded","Refunded","Voided"},SelectedIndex=0,Width=146};
  var method=new ComboBox{ItemsSource=new[]{"All payments","Cash","GCash","Bank transfer","Card","Split payment"},SelectedIndex=0,Width=144};
  var table=new DataGrid{AutoGenerateColumns=false,SelectionMode=DataGridSelectionMode.Single,BorderThickness=new Thickness(0),Background=Brushes.White,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,EnableRowVirtualization=true,EnableColumnVirtualization=true};
  void Column(string title,string property,double weight,double minimum,bool money=false)
  {
   var binding=new Binding(property);if(money)binding.Converter=new HistoryMoneyConverter();
   var column=new DataGridTextColumn{Header=title,Binding=binding,Width=new DataGridLength(weight,DataGridLengthUnitType.Star),MinWidth=minimum};
   if(money)AlignNumberColumn(column);
   else
   {
    var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis));style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center));style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty,new Binding(property)));column.ElementStyle=style;
   }
   table.Columns.Add(column);
  }
  Column("Transaction no.","Transaction",1.8,180);Column("Date & time","Date",1.3,138);Column("Customer","CustomerName",1.2,106);Column("Payment","Method",1,100);Column("Total","Total",1,104,true);
  var badge=new FrameworkElementFactory(typeof(ContentControl));badge.SetBinding(ContentControl.ContentProperty,new Binding("Status"){Converter=new HistoryStatusConverter()});
  badge.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
  table.Columns.Add(new DataGridTemplateColumn{Header="Status",CellTemplate=new DataTemplate{VisualTree=badge},Width=new DataGridLength(1,DataGridLengthUnitType.Star),MinWidth=110});
  var action=new FrameworkElementFactory(typeof(Button));action.SetValue(Button.ContentProperty,"View");action.SetValue(FrameworkElement.StyleProperty,(Style)Application.Current.FindResource("SecondaryButton"));action.SetValue(Control.FontSizeProperty,11d);action.SetValue(Control.PaddingProperty,new Thickness(10,4,10,4));action.SetValue(FrameworkElement.MinHeightProperty,28d);action.SetBinding(Button.CommandProperty,new Binding("OpenCommand"));
  table.Columns.Add(new DataGridTemplateColumn{Header="Actions",CellTemplate=new DataTemplate{VisualTree=action},Width=new DataGridLength(74),MinWidth=74});

  var count=Ui.Text("",12,"#77727B");var period=Ui.Text("",12,"#77727B");
  var empty=Ui.EmptyState("No transactions to show","Choose another period or clear your filters. Completed sales will appear here.","\uE8A5");
  var tableHost=new Grid();tableHost.Children.Add(table);tableHost.Children.Add(empty);
  var ledger=new Grid();ledger.RowDefinitions.Add(new(){Height=GridLength.Auto});ledger.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});ledger.RowDefinitions.Add(new(){Height=GridLength.Auto});
  var ledgerHeading=Ui.Stack(Ui.Text("Sales ledger",16,"#344052",true),period);ledgerHeading.Margin=new Thickness(18,14,18,12);ledger.Children.Add(ledgerHeading);Grid.SetRow(tableHost,1);ledger.Children.Add(tableHost);count.Margin=new Thickness(18,12,18,14);Grid.SetRow(count,2);ledger.Children.Add(count);
  var ledgerCard=Ui.Card(ledger,new Thickness(0));ledgerCard.Padding=new Thickness(0);
  var detailsBody=new StackPanel();var detailActions=new StackPanel();
  var drawer=new Grid();drawer.RowDefinitions.Add(new(){Height=GridLength.Auto});drawer.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});drawer.RowDefinitions.Add(new(){Height=GridLength.Auto});
  var drawerHeader=new Grid{Margin=new Thickness(0,0,0,14)};drawerHeader.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});drawerHeader.ColumnDefinitions.Add(new(){Width=GridLength.Auto});drawerHeader.Children.Add(Ui.Text("Transaction details",16,"#344052",true));
  var close=Ui.Button("×",()=>{table.SelectedItem=null;});close.Width=32;close.MinWidth=32;close.MinHeight=28;close.Padding=new Thickness(4);close.Margin=new Thickness(8,0,0,0);close.ToolTip="Close transaction details";Grid.SetColumn(close,1);drawerHeader.Children.Add(close);drawer.Children.Add(drawerHeader);
  var detailScroll=Ui.Scroll(detailsBody);Grid.SetRow(detailScroll,1);drawer.Children.Add(detailScroll);detailActions.Margin=new Thickness(0,14,0,0);Grid.SetRow(detailActions,2);drawer.Children.Add(detailActions);
  var drawerCard=Ui.Card(drawer,new Thickness(12,0,0,0));drawerCard.Padding=new Thickness(18);drawerCard.Visibility=Visibility.Collapsed;
  var center=new Grid();center.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});var drawerColumn=new ColumnDefinition{Width=new GridLength(0)};center.ColumnDefinitions.Add(drawerColumn);center.Children.Add(ledgerCard);Grid.SetColumn(drawerCard,1);center.Children.Add(drawerCard);
  var allSales=new List<Sale>();var customers=new Dictionary<Guid,string>();var cashierNames=new Dictionary<Guid,string>();var payments=new List<Payment>();var items=new List<SaleItem>();var charges=new List<SaleItemCharge>();var receipts=new List<ReceiptRecord>();var packages=new List<CustomerPackage>();
  var selectedId=(Guid?)null;

  void CloseDrawer()
  {
   drawerCard.Visibility=Visibility.Collapsed;drawerColumn.Width=new GridLength(0);selectedId=null;
  }
  void ShowDetails(Sale? sale)
  {
   if(sale==null){CloseDrawer();return;}
   _auth.Require(_user!);
   var animate=selectedId!=sale.Id;selectedId=sale.Id;drawerCard.Visibility=Visibility.Visible;drawerColumn.Width=new GridLength(350);
   detailsBody.Children.Clear();detailActions.Children.Clear();
   var saleItems=items.Where(i=>i.SaleId==sale.Id).ToList();var salePayments=payments.Where(p=>p.SaleId==sale.Id).ToList();var receipt=receipts.FirstOrDefault(r=>r.SaleId==sale.Id);
   var transaction=Ui.Text(sale.TransactionNumber,17,"#A85E67",true);transaction.ToolTip=sale.TransactionNumber;transaction.Margin=new Thickness(0,0,0,8);detailsBody.Children.Add(transaction);
   detailsBody.Children.Add((UIElement)new HistoryStatusConverter().Convert(sale.Status,typeof(object),null!,CultureInfo.CurrentCulture));
   var metadata=Ui.Stack(Ui.Text(SalonTime.FromUtc(sale.CompletedAtUtc).ToString("dd MMM yyyy · h:mm tt"),12,"#77727B"),Ui.Text("Customer · "+(sale.CustomerId.HasValue?customers.GetValueOrDefault(sale.CustomerId.Value,"Customer"):"Walk-in customer"),13,"#344052",true),Ui.Text("Cashier · "+cashierNames.GetValueOrDefault(sale.CashierUserId,sale.CashierUserId==_user!.Id?_user.Username:"Recorded cashier"),12,"#77727B"));metadata.Margin=new Thickness(0,12,0,16);detailsBody.Children.Add(metadata);
   detailsBody.Children.Add(Ui.Text("PURCHASED ITEMS",10,"#77727B",true));
   foreach(var item in saleItems)
   {
    var line=Ui.Stack(Ui.Text(item.Name,13,"#344052",true),Ui.Text($"{item.Quantity:0.######} × {Ui.Money(item.UnitPrice)} · {item.Kind}",11,"#77727B"),ValueRow("Base amount",item.BaseAmount));
    foreach(var charge in charges.Where(c=>c.SaleItemId==item.Id))
    {
     line.Children.Add(ValueRow("+ "+charge.Name,charge.Total));if(!string.IsNullOrWhiteSpace(charge.Description))line.Children.Add(Ui.Text(charge.Description,11,"#77727B"));
    }
    if(item.ChargeAmount>0||item.DiscountAmount>0||item.OrderDiscountAmount>0)line.Children.Add(ValueRow("Subtotal",item.Subtotal));
    if(item.DiscountAmount>0)line.Children.Add(ValueRow($"Discount ({item.DiscountPercent:0.##}%)",-item.DiscountAmount));
    if(item.OrderDiscountAmount>0)line.Children.Add(ValueRow($"Transaction discount ({sale.DiscountPercent:0.##}%)",-item.OrderDiscountAmount));
    line.Children.Add(ValueRow("Line total",item.Total));var itemCard=new Border{Child=line,Padding=new Thickness(12),Margin=new Thickness(0,10,0,0),Background=Ui.Brush("#FAF6F5"),CornerRadius=new CornerRadius(10)};detailsBody.Children.Add(itemCard);
   }
   var paymentTitle=Ui.Text("PAYMENT DETAILS",10,"#77727B",true);paymentTitle.Margin=new Thickness(0,20,0,0);detailsBody.Children.Add(paymentTitle);
   if(salePayments.Count==0)detailsBody.Children.Add(Ui.Text(sale.Total==0?"No payment was due for this transaction.":"No payment information is recorded.",12,"#77727B"));
   foreach(var payment in salePayments)
   {
    detailsBody.Children.Add(ValueRow(HistoryPaymentLabel(payment.Method)+" received",payment.Amount));
    if(payment.AppliedAmount!=payment.Amount)detailsBody.Children.Add(ValueRow("Applied to this sale",payment.AppliedAmount));
    if(!string.IsNullOrWhiteSpace(payment.Reference))detailsBody.Children.Add(Ui.Text("Reference · "+payment.Reference,11,"#77727B"));
   }
   var totals=Ui.Stack(ValueRow("Subtotal",sale.Subtotal),ValueRow("Total discounts",-sale.DiscountAmount),ValueRow("Amount received",sale.AmountReceived),ValueRow("Change",sale.Change));
   var totalRow=new Grid{Margin=new Thickness(0,14,0,0)};totalRow.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});totalRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});totalRow.Children.Add(Ui.Text("Total",15,"#344052",true));var totalAmount=Ui.Text(Ui.Money(sale.Total),21,"#A85E67",true);Grid.SetColumn(totalAmount,1);totalRow.Children.Add(totalAmount);totals.Children.Add(totalRow);
   detailsBody.Children.Add(new Border{Background=Ui.Brush("#F3E7E7"),CornerRadius=new CornerRadius(10),Padding=new Thickness(14,4,14,14),Margin=new Thickness(0,18,0,0),Child=totals});
   if(!string.IsNullOrWhiteSpace(sale.DiscountReason))detailsBody.Children.Add(Ui.Text("Discount reason · "+sale.DiscountReason,12,"#77727B"));
   if(!string.IsNullOrWhiteSpace(sale.Notes))detailsBody.Children.Add(Ui.Text("Notes · "+sale.Notes,12,"#77727B"));
   if(receipt!=null)
   {
    var preview=new TextBox{Text=receipt.Content,IsReadOnly=true,FontFamily=new FontFamily("Consolas"),FontSize=11,TextWrapping=TextWrapping.Wrap,Height=180,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0,8,0,0)};
    detailsBody.Children.Add(new Expander{Header="Receipt preview",Content=preview,Margin=new Thickness(0,16,0,6)});
   }
   var receiptActions=new UniformGrid{Columns=2};var viewReceipt=Ui.Button("View receipt",()=>ShowReceipt(sale.Id));var printReceipt=Ui.Button("Print receipt",()=>PrintHistoryReceipt(sale));
   printReceipt.IsEnabled=receipt!=null&&(receipt.PrintCount==0||_user!.Role==RoleKind.Administrator);printReceipt.ToolTip=printReceipt.IsEnabled?"Choose a printer and print this receipt":"Administrator access is required to reprint a receipt";
   receiptActions.Children.Add(viewReceipt);receiptActions.Children.Add(printReceipt);detailActions.Children.Add(receiptActions);
   var lineIds=saleItems.Select(i=>i.Id).ToHashSet();
   var refundable=sale.Status==SaleStatus.Completed&&!saleItems.Any(i=>i.Kind==SaleItemKind.PackageRedemption)&&!packages.Any(p=>lineIds.Contains(p.SaleItemId)&&p.UsedSessions>0);
   var allowed=_user!.Role==RoleKind.Administrator&&refundable;
   var sensitive=new UniformGrid{Columns=2,Margin=new Thickness(0,8,0,0)};
   var refund=Ui.WarningButton("Refund",()=>RefundSalePrompt(sale,false));var voidSale=Ui.DangerButton("Void",()=>RefundSalePrompt(sale,true));
   var explanation=_user.Role!=RoleKind.Administrator?"Administrator access is required":sale.Status!=SaleStatus.Completed?"This transaction has already been refunded or voided":!refundable?"Redeemed package sessions cannot be fully reversed":"Requires a reason and confirmation";
   refund.IsEnabled=allowed;voidSale.IsEnabled=allowed;refund.ToolTip=explanation;voidSale.ToolTip=explanation;ToolTipService.SetShowOnDisabled(refund,true);ToolTipService.SetShowOnDisabled(voidSale,true);ToolTipService.SetShowOnDisabled(printReceipt,true);sensitive.Children.Add(refund);sensitive.Children.Add(voidSale);detailActions.Children.Add(sensitive);
   if(animate)Ui.Enter(drawer);
  }
  void ApplyFilters()
  {
   var from=(start.SelectedDate??SalonTime.Today.AddDays(-30)).Date;var until=(end.SelectedDate??SalonTime.Today).Date.AddDays(1);
   if(until<=from)throw new InvalidOperationException("End date must be on or after start date.");
   var fromUtc=SalonTime.ToUtc(from);var untilUtc=SalonTime.ToUtc(until);var oldId=selectedId;
   allSales=_business.List<Sale>(_user!);customers=_business.List<Customer>(_user!).ToDictionary(c=>c.Id,c=>c.FullName);payments=_business.List<Payment>(_user!);items=_business.List<SaleItem>(_user!);charges=_business.List<SaleItemCharge>(_user!);receipts=_business.List<ReceiptRecord>(_user!);packages=_business.List<CustomerPackage>(_user!);
   cashierNames=_user!.Role==RoleKind.Administrator?_business.List<UserAccount>(_user).ToDictionary(u=>u.Id,u=>u.Username):new Dictionary<Guid,string>{{_user.Id,_user.Username}};
   var term=search.Text.Trim();var chosenStatus=status.SelectedItem?.ToString()??"All statuses";var chosenMethod=method.SelectedItem?.ToString()??"All payments";
   var filtered=allSales.Where(s=>s.CompletedAtUtc>=fromUtc&&s.CompletedAtUtc<untilUtc).OrderByDescending(s=>s.CompletedAtUtc).Where(s=>
   {
    var customer=s.CustomerId.HasValue?customers.GetValueOrDefault(s.CustomerId.Value,"Customer"):"Walk-in";
    var methods=payments.Where(p=>p.SaleId==s.Id).Select(p=>p.Method).Distinct().ToArray();
    return (term.Length==0||s.TransactionNumber.Contains(term,StringComparison.OrdinalIgnoreCase)||customer.Contains(term,StringComparison.OrdinalIgnoreCase))&&(chosenStatus=="All statuses"||HistoryStatusLabel(s.Status)==chosenStatus)&&(chosenMethod=="All payments"||(chosenMethod=="Split payment"?methods.Length>1:methods.Any(m=>HistoryPaymentLabel(m)==chosenMethod)));
   }).Select(s=>
   {
    var methods=payments.Where(p=>p.SaleId==s.Id).Select(p=>p.Method).Distinct().ToArray();var label=methods.Length==0?(s.Total==0?"No payment due":"Not recorded"):(methods.Length>1?"Split · ":"")+string.Join(", ",methods.Select(HistoryPaymentLabel));
    SaleRow? row=null;row=new SaleRow(s,s.CustomerId.HasValue?customers.GetValueOrDefault(s.CustomerId.Value,"Customer"):"Walk-in",label,new RelayCommand(()=>{table.SelectedItem=row;ShowDetails(s);}));return row!;
   }).ToList();
   table.ItemsSource=filtered;empty.Visibility=filtered.Count==0?Visibility.Visible:Visibility.Collapsed;table.Visibility=filtered.Count==0?Visibility.Collapsed:Visibility.Visible;
   period.Text=$"{from:dd MMM yyyy} – {until.AddDays(-1):dd MMM yyyy} · Philippine time";
   count.Text=$"{filtered.Count:N0} transaction(s) · {filtered.Count(r=>r.Status==SaleStatus.Completed):N0} completed · Select a row for full details";
   var selected=filtered.FirstOrDefault(r=>r.Sale.Id==oldId);table.SelectedItem=selected;if(selected==null)CloseDrawer();else ShowDetails(selected.Sale);
  }
  void PrintHistoryReceipt(Sale sale)
  {
   var receipt=_business.GetReceipt(sale.Id,_user!);if(receipt.PrintCount>0)_auth.Require(_user!,true);
   var print=new PrintDialog();ReceiptPrinterSupport.ConfigureDialog(print,_business.GetSetting("PrinterName"));
   if(print.ShowDialog()!=true)return;
   var selected=ReceiptPrinterSupport.RequireReady(print.PrintQueue.FullName);
   var width=ReceiptPrinterSupport.ResolveWidth(_business.GetSetting("ReceiptWidth","80"),_business.GetSetting("ReceiptPaperMode","Auto")=="Auto",selected.Name+" "+selected.Driver);
   var document=ReceiptDocument(receipt.Content,width,false,receipt.ReceiptNumber,receipt.PrintCount>0);
   print.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator,receipt.ReceiptNumber);
   _business.RecordReceiptPrint(sale.Id,print.PrintQueue.FullName,width,_user!);ApplyFilters();Ui.Toast("Receipt sent to the printer");
  }
  void RefundSalePrompt(Sale sale,bool isVoid)
  {
   _auth.Require(_user!,true);
   var reason=Ui.Input();var refundMethod=new ComboBox{ItemsSource=Enum.GetValues<PaymentMethod>(),SelectedIndex=0};
   var returned=new CheckBox{Content="Return retail products to inventory",IsChecked=true,Margin=new Thickness(0,8,0,14)};
   var form=Ui.Stack(Ui.Text(sale.TransactionNumber+" · "+Ui.Money(sale.Total),14,"#A85E67",true),Ui.Text("This reverses the full transaction. Used service materials and paid commissions remain recorded costs. Packages with used sessions cannot be fully refunded.",12,"#77727B"),Ui.Field("Reason (required)",reason),Ui.Field("Refund payment method",refundMethod),returned);
   var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};var dialog=Ui.Dialog(isVoid?"Void sale":"Refund sale",form,570,570,actions);var processing=false;
   Button? confirm=null;
   void Commit()
   {
    if(processing)return;
    if(string.IsNullOrWhiteSpace(reason.Text))throw new InvalidOperationException("Enter a reason before continuing.");
    if(!Ui.Confirm(isVoid?"Confirm void":"Confirm refund",$"Record a full {(isVoid?"void":"refund")} of {Ui.Money(sale.Total)} for {sale.TransactionNumber}?",isVoid?"Void transaction":"Record refund",true))return;
    processing=true;confirm!.IsEnabled=false;
    try{var refund=_business.RefundSale(sale.Id,reason.Text,(PaymentMethod)refundMethod.SelectedItem,_user!,isVoid,returned.IsChecked==true);dialog.Close();ApplyFilters();Ui.Toast($"Recorded {refund.RefundNumber}","info");}
    finally{processing=false;confirm.IsEnabled=true;}
   }
   confirm=isVoid?Ui.DangerButton("Confirm void",Commit):Ui.WarningButton("Confirm refund",Commit);confirm.IsEnabled=false;
   reason.TextChanged+=(_,_)=>confirm.IsEnabled=!processing&&!string.IsNullOrWhiteSpace(reason.Text);
   var cancel=Ui.Button("Cancel",()=>dialog.Close());cancel.Margin=new Thickness(0,0,12,0);confirm.Margin=new Thickness(0);actions.Children.Add(cancel);actions.Children.Add(confirm);dialog.ShowDialog();
  }
  table.SelectionChanged+=(_,_)=>Ui.Guard(()=>ShowDetails((table.SelectedItem as SaleRow)?.Sale));
  table.MouseDoubleClick+=(_,_)=>{if(table.SelectedItem is SaleRow row)Ui.Guard(()=>ShowReceipt(row.Sale.Id));};
  search.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){e.Handled=true;Ui.Guard(ApplyFilters);}};
  var filters=new WrapPanel{Margin=new Thickness(0,14,0,12)};filters.Children.Add(CompactFilter("Search transaction or customer",search));filters.Children.Add(CompactFilter("From",start));filters.Children.Add(CompactFilter("To",end));filters.Children.Add(CompactFilter("Status",status));filters.Children.Add(CompactFilter("Payment",method));
  var apply=Ui.Button("Apply",ApplyFilters,true);apply.Margin=new Thickness(0,22,8,8);filters.Children.Add(apply);
  var reset=Ui.Button("Reset",()=>{search.Clear();start.SelectedDate=SalonTime.Today.AddDays(-30);end.SelectedDate=SalonTime.Today;status.SelectedIndex=0;method.SelectedIndex=0;ApplyFilters();});reset.Margin=new Thickness(0,22,0,8);filters.Children.Add(reset);
  var header=Ui.Stack(Ui.Text("Transaction History",24,"#344052",true),Ui.Text("Review completed sales, print receipts, and manage authorized refunds.",13,"#77727B"),filters);
  var root=new Grid();root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});root.Children.Add(header);Grid.SetRow(center,1);root.Children.Add(center);
  var history=Ui.Dialog("Transaction history",root,1280,760);history.MinWidth=Math.Min(900,SystemParameters.WorkArea.Width-40);history.MinHeight=Math.Min(560,SystemParameters.WorkArea.Height-48);
  if(history.Content is ScrollViewer oldScroll&&oldScroll.Content is Border oldSurface){oldSurface.Child=null;oldScroll.Content=null;}
  history.Content=new Border{Padding=new Thickness(20),Background=Ui.Brush("#FAF6F5"),Child=root};history.Loaded+=(_,_)=>Ui.Enter(root);
  ApplyFilters();
  if(Ui.SmokeDialogObserver!=null&&table.Items.Count>0)table.SelectedIndex=0;
  history.ShowDialog();
 }
 private sealed class HistoryMoneyConverter:IValueConverter
 {
  public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is decimal money?Ui.Money(money):"—";
  public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
 }
 private sealed class HistoryStatusConverter:IValueConverter
 {
  public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
  {
   var status=value is SaleStatus saleStatus?saleStatus:SaleStatus.Completed;
   var colors=status switch{SaleStatus.Completed=>("#EAF5F0","#27845F"),SaleStatus.PartiallyRefunded=>("#FFF5E8","#C88B35"),_=>("#FBEAEC","#C84D58")};
   return Ui.Badge(HistoryStatusLabel(status),colors.Item1,colors.Item2,10);
  }
  public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
 }
}
