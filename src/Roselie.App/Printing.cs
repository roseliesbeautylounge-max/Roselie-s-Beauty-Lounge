using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Roselie.Core.Models;

namespace Roselie.App;

public sealed partial class MainWindow
{
 private void ShowReceipt(Guid saleId,bool success=false)
 {
  var receipt=_business.GetReceipt(saleId,_user!);
  var sale=_business.List<Sale>(_user!).Single(s=>s.Id==saleId);
  var items=_business.List<SaleItem>(_user!).Where(i=>i.SaleId==saleId).ToList();
  var itemIds=items.Select(i=>i.Id).ToHashSet();
  var charges=_business.List<SaleItemCharge>(_user!).Where(c=>itemIds.Contains(c.SaleItemId)).ToLookup(c=>c.SaleItemId);
  var payments=_business.List<Payment>(_user!).Where(p=>p.SaleId==saleId).ToList();
  var snapshot=ReceiptSnapshot.Read(receipt.Content);
  var customer=snapshot.Customer;
  if(customer.Length==0)customer=sale.CustomerId.HasValue?_business.List<Customer>(_user!).FirstOrDefault(c=>c.Id==sale.CustomerId)?.FullName??"Customer unavailable":"Walk-in Customer";
  var cashier=snapshot.Cashier.Length>0?snapshot.Cashier:sale.CashierUserId==_user!.Id?_user.Username:"Recorded cashier";
  var method=payments.Count==0?"No payment required":string.Join(" + ",payments.Select(p=>PaymentCaption(p.Method)).Distinct());
  var printNote=Ui.Text("",12,"#77727B");

  var identity=new Grid{Margin=new Thickness(0,0,0,14)};
  identity.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  identity.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  identity.Children.Add(Ui.Stack(Ui.Text("Transaction No. "+sale.TransactionNumber,14,"#344052",true),Ui.Text(SalonTime.FromUtc(sale.CompletedAtUtc).ToString("dd MMM yyyy, h:mm tt"),12,"#77727B")));
  var badge=Ui.StatusPill(HistoryStatusLabel(sale.Status));Grid.SetColumn(badge,1);identity.Children.Add(badge);

  var summary=new DockPanel();
  var summaryTop=Ui.Stack(Ui.Text("Transaction Summary",19,"#344052",true),ReceiptInfo("Customer",customer),ReceiptInfo("Cashier",cashier),ReceiptInfo("Payment",method));
  summaryTop.Margin=new Thickness(0,0,0,14);DockPanel.SetDock(summaryTop,Dock.Top);summary.Children.Add(summaryTop);
  var totals=Ui.Stack(new Border{Height=1,Background=Ui.Brush("#E9DADB"),Margin=new Thickness(0,12,0,9)},ReceiptAmount("Subtotal",sale.Subtotal),ReceiptAmount("Discount",sale.DiscountAmount>0?-sale.DiscountAmount:0),ReceiptAmount("Total",sale.Total,true),ReceiptAmount("Amount received",sale.AmountReceived),ReceiptAmount("Change",sale.Change),printNote);
  printNote.Margin=new Thickness(0,12,0,0);DockPanel.SetDock(totals,Dock.Bottom);summary.Children.Add(totals);
  var itemList=new StackPanel();
  foreach(var item in items)
  {
   var itemContent=new StackPanel();
   var title=ReceiptValue(item.Name,Ui.Money(item.Total),true);itemContent.Children.Add(title);
   var baseDescription=Ui.Text($"{item.Quantity:0.######} × {Ui.Money(item.UnitPrice)}",12,"#77727B");baseDescription.Margin=new Thickness(0,3,0,6);itemContent.Children.Add(baseDescription);
   foreach(var charge in charges[item.Id])
   {
    var chargeRow=ReceiptValue("+ "+charge.Name,Ui.Money(charge.Total));chargeRow.Margin=new Thickness(10,3,0,3);itemContent.Children.Add(chargeRow);
    if(!string.IsNullOrWhiteSpace(charge.Description)){var description=Ui.Text(charge.Description,11,"#77727B");description.Margin=new Thickness(10,0,0,4);itemContent.Children.Add(description);}
   }
   if(item.DiscountAmount>0)itemContent.Children.Add(ReceiptValue($"Item discount ({item.DiscountPercent:0.##}%)","− "+Ui.Money(item.DiscountAmount)));
   if(item.OrderDiscountAmount>0)itemContent.Children.Add(ReceiptValue($"Transaction discount ({sale.DiscountPercent:0.##}%)","− "+Ui.Money(item.OrderDiscountAmount)));
   var itemCard=Ui.Card(itemContent,new Thickness(0,0,0,10));itemCard.Padding=new Thickness(12);itemList.Children.Add(itemCard);
  }
  if(items.Count==0)itemList.Children.Add(Ui.Text("Item details are available in the saved receipt.",13,"#77727B"));
  summary.Children.Add(Ui.Scroll(itemList));
  var summaryCard=Ui.Card(summary,new Thickness(0));summaryCard.Padding=new Thickness(20);

  var previewDocument=ReceiptDocument(receipt.Content,80,true,receipt.ReceiptNumber,receipt.PrintCount>0);
  var viewer=new FlowDocumentScrollViewer{Document=previewDocument,IsToolBarVisible=false,Background=Brushes.White,BorderThickness=new Thickness(0),VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
  var paper=Ui.Card(viewer,new Thickness(0));paper.Padding=new Thickness(8);paper.MaxWidth=420;paper.HorizontalAlignment=HorizontalAlignment.Center;
  var preview=new DockPanel();
  var previewLabel=Ui.Text("Receipt Preview",19,"#344052",true);previewLabel.Margin=new Thickness(0,0,0,10);DockPanel.SetDock(previewLabel,Dock.Top);preview.Children.Add(previewLabel);
  preview.Children.Add(new Border{Background=Ui.Brush("#F3E7E7"),CornerRadius=new CornerRadius(12),Padding=new Thickness(16),Child=paper});
  var columns=new Grid();columns.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});columns.ColumnDefinitions.Add(new(){Width=new GridLength(24)});columns.ColumnDefinitions.Add(new(){Width=new GridLength(1.06,GridUnitType.Star)});
  columns.Children.Add(summaryCard);Grid.SetColumn(preview,2);columns.Children.Add(preview);
  var body=new Grid();body.RowDefinitions.Add(new(){Height=GridLength.Auto});body.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
  body.Children.Add(identity);Grid.SetRow(columns,1);body.Children.Add(columns);

  var actions=new Grid();actions.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});actions.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var dialog=Ui.Dialog(success?"Payment Successful":"Transaction Receipt",body,1140,820,actions);
  dialog.MinWidth=Math.Min(900,SystemParameters.WorkArea.Width-40);
  // Keep the complete modal stationary. Only long item lists and thermal paper scroll.
  var bodyScroll=dialog.Content is Grid root?root.Children.OfType<ScrollViewer>().FirstOrDefault():dialog.Content as ScrollViewer;
  if(bodyScroll!=null)
  {
   var inset=bodyScroll.Content is Border surface?surface.Padding.Top+surface.Padding.Bottom:48;
   void FitBody()=>body.Height=Math.Max(120,bodyScroll.ActualHeight-inset);
   bodyScroll.SizeChanged+=(_,_)=>FitBody();dialog.Loaded+=(_,_)=>FitBody();
  }

  ReceiptRecord AuthorizePrint()
  {
   var current=_business.GetReceipt(saleId,_user!);
   if(current.PrintCount>0)_auth.Require(_user!,true);
   return current;
  }
  Button? windowsPrint=null,thermalPrint=null;
  void RefreshPrintState()
  {
   var current=_business.GetReceipt(saleId,_user!);
   var needsAdmin=current.PrintCount>0&&_user!.Role!=RoleKind.Administrator;
   printNote.Text=needsAdmin?"An administrator can authorize a reprint.":current.PrintCount==0?"Saved receipt · Ready to print":"Printed "+current.PrintCount+" time"+(current.PrintCount==1?"":"s")+" · Reprints are audited";
   if(windowsPrint!=null)windowsPrint.IsEnabled=!needsAdmin;
   if(thermalPrint!=null)thermalPrint.IsEnabled=!needsAdmin;
  }
  windowsPrint=Ui.Button("Print Receipt",()=>
  {
   var printable=AuthorizePrint();
   var print=new PrintDialog();
   ReceiptPrinterSupport.ConfigureDialog(print,_business.GetSetting("PrinterName"));
   if(print.ShowDialog()!=true)return;
   var selected=ReceiptPrinterSupport.RequireReady(print.PrintQueue.FullName);
   var width=ReceiptPrinterSupport.ResolveWidth(_business.GetSetting("ReceiptWidth","80"),_business.GetSetting("ReceiptPaperMode","Auto")=="Auto",selected.Name+" "+selected.Driver);
   var document=ReceiptDocument(printable.Content,width,false,printable.ReceiptNumber,printable.PrintCount>0);
   print.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,printable.ReceiptNumber);
   _business.RecordReceiptPrint(saleId,print.PrintQueue.FullName,width,_user!);
   RefreshPrintState();Ui.Toast("Receipt sent to printer");
  },true);
  thermalPrint=Ui.Button("ESC/POS Print",()=>
  {
   var printable=AuthorizePrint();
   var printer=ReceiptPrinterSupport.RequireReady(_business.GetSetting("PrinterName"));
   if(printer.IsVirtual)throw new InvalidOperationException("ESC/POS needs a physical receipt printer. Use Print Receipt for this Windows printer.");
   var width=ReceiptPrinterSupport.ResolveWidth(_business.GetSetting("ReceiptWidth","80"),_business.GetSetting("ReceiptPaperMode","Auto")=="Auto",printer.Name+" "+printer.Driver);
   RawPrinter.Send(printer.Name,printable.Content,printable.ReceiptNumber,width,printer.IsEpsonU220);
   _business.RecordReceiptPrint(saleId,printer.Name,width,_user!);
   RefreshPrintState();Ui.Toast("Receipt sent to receipt printer");
  });
  thermalPrint.ToolTip="Send the original saved receipt directly to the configured ESC/POS receipt printer";
  StyleReceiptAction(thermalPrint);thermalPrint.Margin=new Thickness(0);thermalPrint.HorizontalAlignment=HorizontalAlignment.Left;actions.Children.Add(thermalPrint);
  var right=Ui.Row();right.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(right,1);actions.Children.Add(right);
  StyleReceiptAction(windowsPrint);windowsPrint.Margin=new Thickness(0,0,12,0);right.Children.Add(windowsPrint);
  var done=Ui.Button("Done",()=>dialog.Close());StyleReceiptAction(done);done.MinWidth=84;done.Margin=new Thickness(0);done.IsDefault=true;right.Children.Add(done);
  RefreshPrintState();dialog.ShowDialog();
 }

 private static string PaymentCaption(PaymentMethod method)=>method==PaymentMethod.BankTransfer?"Bank Transfer":method.ToString();
 private static void StyleReceiptAction(Button button){button.MinHeight=40;button.MinWidth=132;button.Padding=new Thickness(12,8,12,8);}
 private static Grid ReceiptInfo(string label,string value)
 {
  var row=ReceiptValue(label,value);row.Margin=new Thickness(0,9,0,0);return row;
 }
 private static Grid ReceiptAmount(string label,decimal value,bool total=false)
 {
  var row=ReceiptValue(label,Ui.Money(value),total);row.Margin=new Thickness(0,total?9:4,0,total?9:4);
  if(total){((TextBlock)row.Children[0]).FontSize=17;((TextBlock)row.Children[1]).FontSize=24;((TextBlock)row.Children[1]).Foreground=Ui.Brush("#A85E67");}
  return row;
 }
 private static Grid ReceiptValue(string label,string value,bool strong=false)
 {
  var row=new Grid();row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
  var caption=Ui.Text(label,strong?14:12,strong?"#344052":"#77727B",strong);caption.Margin=new Thickness(0,0,12,0);caption.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(caption);
  var amount=Ui.Text(value,strong?15:12,"#344052",strong);amount.TextAlignment=TextAlignment.Right;amount.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(amount,1);row.Children.Add(amount);return row;
 }

 private static FlowDocument ReceiptDocument(string text,double width,bool preview=false,string? receiptNumber=null,bool reprint=false)
 {
  var snapshot=ReceiptSnapshot.Read(text);
  var font=preview?11.5:width<=58?9:10;
  var pageWidth=preview?380:width/25.4*96;
  var leftMargin=preview?12:width==76?6.8/25.4*96:width==69.5?6.7/25.4*96:width==57.5?4.2/25.4*96:5;
  var rightMargin=preview?12:width is 76 or 69.5 or 57.5?5.8/25.4*96:5;
  var doc=new FlowDocument{FontFamily=new FontFamily("Segoe UI"),FontSize=font,Foreground=Brushes.Black,PagePadding=new Thickness(leftMargin,preview?12:5,rightMargin,preview?12:5),PageWidth=pageWidth,ColumnWidth=pageWidth-leftMargin-rightMargin,ColumnGap=0};
  var logo=Ui.CreateLogoImage(preview?120:80,preview?56:42);
  if(!preview&&logo.Source is BitmapSource source)logo.Source=MonochromeLogo(source);
  if(logo.Source!=null){logo.HorizontalAlignment=HorizontalAlignment.Center;doc.Blocks.Add(new BlockUIContainer(logo){Margin=new Thickness(0,0,0,6)});}
  void ParagraphLine(string value,bool bold=false,bool centered=false,double? size=null,Thickness? margin=null)
  {
   if(value.Length==0)return;
   doc.Blocks.Add(new Paragraph(new Run(value)){FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,FontSize=size??font,TextAlignment=centered?TextAlignment.Center:TextAlignment.Left,Margin=margin??new Thickness(0,0,0,4)});
  }
  void Separator()=>doc.Blocks.Add(new Paragraph{BorderBrush=Brushes.Black,BorderThickness=new Thickness(0,0,0,0.6),Margin=new Thickness(0,6,0,6),FontSize=1,LineHeight=1});
  void AmountLine(string label,string amount,bool strong=false,double indent=0)
  {
   var table=new Table{CellSpacing=0,Margin=new Thickness(indent,0,0,4),FontWeight=strong?FontWeights.SemiBold:FontWeights.Normal,FontSize=strong&&label.Equals("TOTAL",StringComparison.OrdinalIgnoreCase)?font+2:font};
   // FlowDocument tables do not distribute star columns like a WPF Grid.
   // Explicit paper-relative widths keep amounts and names inside thermal margins.
   var amountWidth=preview?120:width<=58?82:95;
   table.Columns.Add(new TableColumn{Width=new GridLength(doc.ColumnWidth-indent-amountWidth)});
   table.Columns.Add(new TableColumn{Width=new GridLength(amountWidth)});
   var rows=new TableRowGroup();var row=new TableRow();rows.Rows.Add(row);table.RowGroups.Add(rows);
   row.Cells.Add(new TableCell(new Paragraph(new Run(label)){Margin=new Thickness(0)}){Padding=new Thickness(0,0,8,0)});
   row.Cells.Add(new TableCell(new Paragraph(new Run(amount)){Margin=new Thickness(0),TextAlignment=TextAlignment.Right}){Padding=new Thickness(0)});
   doc.Blocks.Add(table);
  }
  void MetadataLine(string label,string value)
  {
   var table=new Table{CellSpacing=0,Margin=new Thickness(0,0,0,4),FontSize=font-1};
   var labelWidth=preview?90:width<=58?54:68;
   table.Columns.Add(new TableColumn{Width=new GridLength(labelWidth)});table.Columns.Add(new TableColumn{Width=new GridLength(doc.ColumnWidth-labelWidth)});
   var group=new TableRowGroup();var row=new TableRow();group.Rows.Add(row);table.RowGroups.Add(group);
   row.Cells.Add(new TableCell(new Paragraph(new Run(label)){Margin=new Thickness(0)}){Padding=new Thickness(0,0,8,0)});
   row.Cells.Add(new TableCell(new Paragraph(new Run(value)){Margin=new Thickness(0)}){Padding=new Thickness(0)});doc.Blocks.Add(table);
  }

  if(!snapshot.Structured)
  {
   // Unknown older receipt formats are rendered in full, rather than losing content.
   foreach(var line in snapshot.Lines)ParagraphLine(line);
   return doc;
  }
  ParagraphLine(snapshot.BusinessName,true,true,font+2,new Thickness(0,0,0,5));
  foreach(var line in snapshot.Header.Skip(1))ParagraphLine(line,false,true,font-1);
  Separator();
  foreach(var line in snapshot.Metadata)
  {
   if(line=="INTERNAL SALES RECEIPT"){ParagraphLine(line,true,true,font);continue;}
   if(line.StartsWith("Cashier:",StringComparison.Ordinal)||line.StartsWith("Customer:",StringComparison.Ordinal))
   {
    var split=line.IndexOf(':');MetadataLine(line[..split],line[(split+1)..].Trim());continue;
   }
   if(line==snapshot.TransactionNumber)
   {
    if(!string.IsNullOrEmpty(receiptNumber))MetadataLine("Receipt No.",receiptNumber);
    MetadataLine("Transaction No.",line);continue;
   }
   if(line==snapshot.DateTime)MetadataLine("Date & time",line);
   else ParagraphLine(line,false,true,font-1);
  }
  if(snapshot.Customer.Length==0)MetadataLine("Customer","Walk-in Customer");
  if(reprint)ParagraphLine("REPRINT · Original transaction",true,true,font);
  Separator();
  foreach(var line in snapshot.Items)
  {
   var value=line.Trim();
   if(!line.StartsWith(' ')){ParagraphLine(value,true,false,font+0.5,new Thickness(0,5,0,3));continue;}
   var quantity=Regex.Match(value,@"^(?<qty>[\d.,]+)\s+x\s+PHP\s+(?<unit>[\d.,]+)\s*=\s*(?<base>[\d.,]+)$",RegexOptions.CultureInvariant);
   if(quantity.Success){AmountLine(quantity.Groups["qty"].Value+" × PHP "+quantity.Groups["unit"].Value,"PHP "+quantity.Groups["base"].Value);continue;}
   if(ReceiptSnapshot.TryAmount(value,out var label,out var amount))
   {
    label=label.Replace("Order discount","Transaction discount",StringComparison.Ordinal);
    AmountLine(label,amount,label.StartsWith("Line total",StringComparison.OrdinalIgnoreCase),label.StartsWith('+')?8:0);
   }
   else ParagraphLine(value,false,false,font-1);
  }
  Separator();
  foreach(var line in snapshot.Totals)
  {
   var value=line.Trim();
   if(value.StartsWith("Thank",StringComparison.OrdinalIgnoreCase)){ParagraphLine(value,false,true,font,new Thickness(0,9,0,3));continue;}
   if(value.StartsWith("Reference:",StringComparison.Ordinal)){ParagraphLine(value,false,false,font-1,new Thickness(8,0,0,4));continue;}
   if(ReceiptSnapshot.TryAmount(value,out var label,out var amount))
   {
    if(label=="BankTransfer")label="Bank Transfer";
    AmountLine(label,amount,label.Equals("TOTAL",StringComparison.OrdinalIgnoreCase));
   }
   else ParagraphLine(value,false,false,font-1);
  }
  return doc;
 }

 internal void SmokeReceiptPrinting(string output)
 {
  var receipt=_business.List<ReceiptRecord>(_user!).First();
  foreach(var width in ReceiptPrinterSupport.PaperWidths)
  {
   var document=ReceiptDocument(receipt.Content,width,false,receipt.ReceiptNumber,true);
   var renderedText=new TextRange(document.ContentStart,document.ContentEnd).Text;
   foreach(var required in new[]{receipt.ReceiptNumber,"Hair Color","1,035","65.00","REPRINT"})
   {
    // Stored numbers may omit grouping separators. Layout must retain their exact value.
    if(!renderedText.Replace(",","").Contains(required.Replace(",",""),StringComparison.Ordinal))throw new InvalidOperationException($"The {width} mm receipt lost saved content: {required}.");
   }
   var paginator=((IDocumentPaginatorSource)document).DocumentPaginator;
   paginator.PageSize=new Size(width/25.4*96,1000);paginator.ComputePageCount();
   if(paginator.PageCount!=1)throw new InvalidOperationException($"The single-service {width} mm receipt should fit on one 1000-pixel page.");
   for(var index=0;index<paginator.PageCount;index++)
   {
    var page=paginator.GetPage(index);var visual=new DrawingVisual();
    using(var drawing=visual.RenderOpen()){drawing.DrawRectangle(Brushes.White,null,new Rect(page.Size));drawing.DrawRectangle(new VisualBrush(page.Visual),null,new Rect(page.Size));}
    var bitmap=new RenderTargetBitmap((int)Math.Ceiling(page.Size.Width*2),(int)Math.Ceiling(page.Size.Height*2),192,192,PixelFormats.Pbgra32);bitmap.Render(visual);
    var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=System.IO.File.Create(System.IO.Path.Combine(output,$"receipt-{width}mm-page-{index+1}.png"));encoder.Save(stream);
   }
  }
  const string legacy="Older receipt format\nCustomer: Original customer\nSaved service PHP 99.99\nReference: 000123";
  var fallback=ReceiptDocument(legacy,58);var fallbackText=new TextRange(fallback.ContentStart,fallback.ContentEnd).Text;
  if(legacy.Split('\n').Any(line=>!fallbackText.Contains(line,StringComparison.Ordinal)))throw new InvalidOperationException("Legacy receipt rendering lost content.");
 }

 private static BitmapSource MonochromeLogo(BitmapSource original)
 {
  var scale=Math.Min(1,640d/Math.Max(original.PixelWidth,original.PixelHeight));
  var scaled=scale<1?new TransformedBitmap(original,new ScaleTransform(scale,scale)):original;
  var source=new FormatConvertedBitmap(scaled,PixelFormats.Bgra32,null,0);
  var stride=source.PixelWidth*4;var pixels=new byte[stride*source.PixelHeight];source.CopyPixels(pixels,stride,0);
  var gray=new byte[source.PixelWidth*source.PixelHeight];
  for(var i=0;i<gray.Length;i++)
  {
   var offset=i*4;var alpha=pixels[offset+3];
   var blue=(pixels[offset]*alpha+255*(255-alpha))/255;
   var green=(pixels[offset+1]*alpha+255*(255-alpha))/255;
   var red=(pixels[offset+2]*alpha+255*(255-alpha))/255;
   gray[i]=(red*299+green*587+blue*114)/1000<224?(byte)0:(byte)255;
  }
  var bitmap=BitmapSource.Create(source.PixelWidth,source.PixelHeight,96,96,PixelFormats.Gray8,null,gray,source.PixelWidth);bitmap.Freeze();return bitmap;
 }
 private void PrintReport(ReportData report)
 {
  var doc=new FlowDocument{FontFamily=new FontFamily("Segoe UI"),FontSize=11,PagePadding=new Thickness(36),ColumnWidth=double.PositiveInfinity};doc.Blocks.Add(new Paragraph(new Run("Roselie's Beauty Lounge")){FontSize=20,FontWeight=FontWeights.Bold});doc.Blocks.Add(new Paragraph(new Run(report.Title+"\n"+report.Period)));
  var table=new Table{CellSpacing=0};foreach(var c in report.Columns)table.Columns.Add(new TableColumn());var group=new TableRowGroup();table.RowGroups.Add(group);void Add(object?[] row,bool header){var r=new TableRow();foreach(var value in row)r.Cells.Add(new TableCell(new Paragraph(new Run(value?.ToString()??""))){Padding=new Thickness(6),BorderBrush=Ui.Brush("#E9DADB"),BorderThickness=new Thickness(0,0,0,1),Background=header?Ui.Brush("#F3E7E7"):Brushes.White});group.Rows.Add(r);}Add(report.Columns.Cast<object?>().ToArray(),true);foreach(var row in report.Rows)Add(row,false);doc.Blocks.Add(table);
  var viewer=new FlowDocumentScrollViewer{Document=doc,IsToolBarVisible=false,Height=430,Background=Brushes.White,BorderThickness=new Thickness(0)};
  var body=Ui.Stack(Ui.Text(report.Title+" · "+report.Period,13,"#77727B"),Ui.Card(viewer,new Thickness(0,16,0,16)));
  var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
  var preview=Ui.Dialog("Report print preview",body,960,760,footer);
  var printButton=Ui.Button("Print report",()=>{var print=new PrintDialog();if(print.ShowDialog()==true){doc.PageWidth=print.PrintableAreaWidth;doc.PageHeight=print.PrintableAreaHeight;print.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,report.Title);Ui.Toast("Report sent to printer");}},true);printButton.Margin=new Thickness(0,0,12,0);footer.Children.Add(printButton);var close=Ui.Button("Close",()=>preview.Close());close.Margin=new Thickness(0);footer.Children.Add(close);preview.ShowDialog();
 }
}

internal sealed class ReceiptSnapshot
{
 public string[] Lines { get; private init; }=[];
 public string[] Header { get; private init; }=[];
 public string[] Metadata { get; private init; }=[];
 public string[] Items { get; private init; }=[];
 public string[] Totals { get; private init; }=[];
 public bool Structured { get; private init; }
 public string BusinessName=>Header.FirstOrDefault()??"";
 public string Cashier=>Field("Cashier:");
 public string Customer=>Field("Customer:");
 public string TransactionNumber=>Metadata.FirstOrDefault(line=>Regex.IsMatch(line,@"^RBL-[A-Za-z0-9-]+$",RegexOptions.CultureInvariant))??"";
 public string DateTime=>Metadata.FirstOrDefault(line=>Regex.IsMatch(line,@"^\d{4}-\d{2}-\d{2}\s",RegexOptions.CultureInvariant))??"";
 private string Field(string prefix)=>(Structured?Metadata:Lines).FirstOrDefault(line=>line.StartsWith(prefix,StringComparison.Ordinal))?[prefix.Length..].Trim()??"";
 public static ReceiptSnapshot Read(string text)
 {
  var lines=text.Replace("\r","").Split('\n');
  var marker=Array.FindIndex(lines,line=>line=="INTERNAL SALES RECEIPT");
  var separators=Enumerable.Range(0,lines.Length).Where(index=>Regex.IsMatch(lines[index],@"^-{8,}$",RegexOptions.CultureInvariant)).ToArray();
  if(marker<0||separators.Length<2||separators[0]<=marker)return new ReceiptSnapshot{Lines=lines};
  return new ReceiptSnapshot{Lines=lines,Structured=true,Header=lines[..marker],Metadata=lines[marker..separators[0]],Items=lines[(separators[0]+1)..separators[1]],Totals=lines[(separators[1]+1)..]};
 }
 public static bool TryAmount(string text,out string label,out string amount)
 {
  var match=Regex.Match(text,@"^(?<label>.+?):\s*(?:PHP\s*)?(?<amount>-?[\d.,]+)$",RegexOptions.CultureInvariant);
  label="";amount="";if(!match.Success)return false;
  label=match.Groups["label"].Value.Trim();var savedAmount=match.Groups["amount"].Value;
  amount=savedAmount.StartsWith('-')?"− PHP "+savedAmount[1..]:"PHP "+savedAmount;
  return true;
 }
}

internal static class RawPrinter
{
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct DocInfo{[MarshalAs(UnmanagedType.LPWStr)]public string Name;[MarshalAs(UnmanagedType.LPWStr)]public string? Output;[MarshalAs(UnmanagedType.LPWStr)]public string DataType;}
 [DllImport("winspool.drv",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool OpenPrinter(string name,out IntPtr printer,IntPtr defaults);
 [DllImport("winspool.drv",SetLastError=true)]private static extern bool ClosePrinter(IntPtr printer);
 [DllImport("winspool.drv",CharSet=CharSet.Unicode,SetLastError=true)]private static extern int StartDocPrinter(IntPtr printer,int level,ref DocInfo doc);
 [DllImport("winspool.drv",SetLastError=true)]private static extern bool EndDocPrinter(IntPtr printer);
 [DllImport("winspool.drv",SetLastError=true)]private static extern bool AbortPrinter(IntPtr printer);
 [DllImport("winspool.drv",SetLastError=true)]private static extern bool StartPagePrinter(IntPtr printer);
 [DllImport("winspool.drv",SetLastError=true)]private static extern bool EndPagePrinter(IntPtr printer);
 [DllImport("winspool.drv",SetLastError=true)]private static extern bool WritePrinter(IntPtr printer,IntPtr bytes,int count,out int written);
 public static void Send(string printerName,string receipt,string title,double width=80,bool epsonU220=false)
 {
  var bytes=ReceiptPrinterSupport.BuildEscPosBytes(receipt,width,epsonU220);
  if(!OpenPrinter(printerName,out var printer,IntPtr.Zero))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
  var documentStarted=false;
  try
  {
   var doc=new DocInfo{Name=title,DataType="RAW"};
   if(StartDocPrinter(printer,1,ref doc)==0)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   documentStarted=true;
   if(!StartPagePrinter(printer))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   var pinned=GCHandle.Alloc(bytes,GCHandleType.Pinned);
   try
   {
    var offset=0;
    while(offset<bytes.Length)
    {
     var remaining=bytes.Length-offset;
     if(!WritePrinter(printer,IntPtr.Add(pinned.AddrOfPinnedObject(),offset),remaining,out var written))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
     if(written<=0||written>remaining)throw new System.IO.IOException("Windows did not accept the complete receipt. The incomplete print job was cancelled.");
     offset+=written;
    }
   }
   finally{pinned.Free();}
   if(!EndPagePrinter(printer))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   if(!EndDocPrinter(printer))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   documentStarted=false;
  }
  catch{if(documentStarted)AbortPrinter(printer);throw;}
  finally{ClosePrinter(printer);}
 }
}
