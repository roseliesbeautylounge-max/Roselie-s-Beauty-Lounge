using System.Globalization;
using System.Printing;
using System.Text;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

[assembly: InternalsVisibleTo("Roselie.Tests")]

namespace Roselie.App;

public sealed record ReceiptPrinter(string Name,string Driver,bool IsDefault,bool IsReady,string Status)
{
 public bool IsEpsonU220=>ReceiptPrinterSupport.IsEpsonU220(Name+" "+Driver);
 public bool IsVirtual=>ReceiptPrinterSupport.IsVirtual(Name+" "+Driver);
}

public static class ReceiptPrinterSupport
{
 public static readonly double[] PaperWidths=[57.5,58,69.5,76,80];
 public static bool IsEpsonU220(string name)=>name.Replace("-","").Replace(" ","").Contains("TMU220",StringComparison.OrdinalIgnoreCase);
 public static bool IsVirtual(string name)=>new[]{"PDF","XPS","OneNote","Fax","Microsoft Print"}.Any(value=>name.Contains(value,StringComparison.OrdinalIgnoreCase));
 public static double ResolveWidth(string savedWidth,bool automatic,string printerName)
 {
  if(automatic&&IsEpsonU220(printerName))return 76;
  var width=double.TryParse(savedWidth,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsed)&&PaperWidths.Contains(parsed)?parsed:80;
  if(IsEpsonU220(printerName)&&width is not (57.5 or 58 or 69.5 or 76))throw new InvalidOperationException("The Epson TM-U220 uses 57.5, 69.5 or 76 mm paper. Choose its paper width in Settings > Printer.");
  return IsEpsonU220(printerName)&&width==58?57.5:width;
 }
 public static ReceiptPrinter? Select(string configured,IReadOnlyList<ReceiptPrinter> printers)
 {
  if(!string.IsNullOrWhiteSpace(configured))return printers.FirstOrDefault(p=>p.Name.Equals(configured,StringComparison.OrdinalIgnoreCase));
  var epson=printers.Where(p=>p.IsEpsonU220).ToList();if(epson.Count==1)return epson[0];
  var physical=printers.Where(p=>!p.IsVirtual).ToList();return physical.FirstOrDefault(p=>p.IsDefault)??(physical.Count==1?physical[0]:null);
 }
 public static IReadOnlyList<ReceiptPrinter> Discover()
 {
  using var server=new LocalPrintServer();string? defaultName=null;
  try{using var defaultQueue=LocalPrintServer.GetDefaultPrintQueue();defaultName=defaultQueue.FullName;}catch(PrintSystemException){}
  var found=new List<ReceiptPrinter>();
  using var queues=server.GetPrintQueues(new[]{EnumeratedPrintQueueTypes.Local,EnumeratedPrintQueueTypes.Connections});
  foreach(var queue in queues)
  {
   using(queue)
   {
    var name="Unavailable Windows printer";
    try
    {
     name=queue.FullName;queue.Refresh();var flags=queue.QueueStatus;
     var blocked=PrintQueueStatus.Offline|PrintQueueStatus.Paused|PrintQueueStatus.Error|PrintQueueStatus.PaperOut|PrintQueueStatus.PaperJam|PrintQueueStatus.DoorOpen|PrintQueueStatus.NotAvailable|PrintQueueStatus.UserIntervention|PrintQueueStatus.PendingDeletion;
     var ready=(flags&blocked)==0;
     var status=ready?"Windows reports ready":(flags&blocked).ToString();
     found.Add(new ReceiptPrinter(name,queue.QueueDriver?.Name??"",string.Equals(name,defaultName,StringComparison.OrdinalIgnoreCase),ready,status));
    }
    catch(Exception ex){found.Add(new ReceiptPrinter(name,"",string.Equals(name,defaultName,StringComparison.OrdinalIgnoreCase),false,"Windows status unavailable: "+ex.GetBaseException().Message));}
   }
  }
  return found.OrderBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).ToArray();
 }
 public static void ConfigureDialog(PrintDialog dialog,string configured)
 {
  var selected=Select(configured,Discover());
  if(selected==null){if(configured.Length>0)throw new InvalidOperationException("The saved printer is not installed. Connect it and refresh Settings > Printer.");return;}
  using var server=new LocalPrintServer();dialog.PrintQueue=server.GetPrintQueue(selected.Name);
 }
 public static ReceiptPrinter RequireReady(string name)
 {
  var printer=Select(name,Discover())??throw new InvalidOperationException("No receipt printer was detected. Connect the printer, install its Windows driver, then refresh Settings > Printer.");
  if(!printer.IsReady)throw new InvalidOperationException(printer.Name+": "+printer.Status+". Check the printer power, cable, paper and cover, then try again.");return printer;
 }
 public static void PrepareDocument(PrintDialog dialog,FlowDocument document,double width)
 {
  if(!PaperWidths.Contains(width))throw new ArgumentException("Unsupported receipt paper width.",nameof(width));
  var queue=dialog.PrintQueue??throw new InvalidOperationException("Choose an installed receipt printer.");
  var original=dialog.PrintTicket??queue.UserPrintTicket??queue.DefaultPrintTicket??new PrintTicket();
  var originalCapabilities=queue.GetPrintCapabilities(original);
  var landscape=original.PageOrientation is PageOrientation.Landscape or PageOrientation.ReverseLandscape;
  var driverLength=PositiveDimension(original.PageMediaSize?.Height,landscape?originalCapabilities.OrientedPageMediaWidth:originalCapabilities.OrientedPageMediaHeight,1056);
  var rollWidth=width/25.4*96;
  var request=original.Clone();request.PageOrientation=PageOrientation.Portrait;
  request.PageMediaSize=new PageMediaSize(rollWidth,driverLength);
  // A Windows driver may replace unsupported custom media. Use its validated ticket and capabilities.
  var validated=queue.MergeAndValidatePrintTicket(original,request).ValidatedPrintTicket;
  var capabilities=queue.GetPrintCapabilities(validated);
  var mediaWidth=PositiveDimension(capabilities.OrientedPageMediaWidth,validated.PageMediaSize?.Width,rollWidth);
  var mediaHeight=PositiveDimension(capabilities.OrientedPageMediaHeight,validated.PageMediaSize?.Height,driverLength);
  var virtualPrinter=IsVirtual(queue.FullName+" "+(queue.QueueDriver?.Name??""));
  if(!virtualPrinter&&Math.Abs(mediaWidth-rollWidth)>3)
   throw new InvalidOperationException($"The Windows driver did not accept {width.ToString("0.0#",CultureInfo.InvariantCulture)} mm paper. Select the matching paper form in the printer's Windows properties and try again.");
  var documentWidth=Math.Min(rollWidth,mediaWidth);
  var imageable=capabilities.PageImageableArea;
  var printable=imageable==null?new Rect(0,0,mediaWidth,mediaHeight):new Rect(imageable.OriginWidth,imageable.OriginHeight,imageable.ExtentWidth,imageable.ExtentHeight);
  var padding=BoundMargins(document.PagePadding,new Size(documentWidth,mediaHeight),printable);
  document.PageWidth=documentWidth;document.PageHeight=mediaHeight;document.PagePadding=padding;
  document.ColumnWidth=documentWidth-padding.Left-padding.Right;
  // The structured receipt uses absolute table widths. Reduce them only if the driver requires larger margins.
  foreach(var table in document.Blocks.OfType<Table>())
  {
   var available=document.ColumnWidth-table.Margin.Left-table.Margin.Right;
   if(available<=0)throw new InvalidOperationException("The receipt cannot fit inside this printer's printable area.");
   if(table.Columns.All(column=>column.Width.IsAbsolute))
   {
    var total=table.Columns.Sum(column=>column.Width.Value);
    if(total>available)foreach(var column in table.Columns)column.Width=new GridLength(column.Width.Value*available/total);
   }
  }
  dialog.PrintTicket=validated;
  ((IDocumentPaginatorSource)document).DocumentPaginator.PageSize=new Size(documentWidth,mediaHeight);
 }
 internal static Thickness BoundMargins(Thickness requested,Size media,Rect printable)
 {
  if(!double.IsFinite(media.Width)||!double.IsFinite(media.Height)||media.Width<=0||media.Height<=0
   ||printable.IsEmpty||!double.IsFinite(printable.X)||!double.IsFinite(printable.Y)||!double.IsFinite(printable.Width)||!double.IsFinite(printable.Height))
   throw new InvalidOperationException("The Windows printer returned an invalid paper or printable-area size.");
  printable.Intersect(new Rect(new Point(0,0),media));
  if(printable.IsEmpty||printable.Width<=0||printable.Height<=0)throw new InvalidOperationException("The printer has no printable area for this receipt paper width.");
  var padding=new Thickness(Math.Max(requested.Left,printable.Left),Math.Max(requested.Top,printable.Top),Math.Max(requested.Right,media.Width-printable.Right),Math.Max(requested.Bottom,media.Height-printable.Bottom));
  if(media.Width-padding.Left-padding.Right<24||media.Height-padding.Top-padding.Bottom<24)
   throw new InvalidOperationException("The printer's margins leave too little room for the receipt. Check its paper form and roll width.");
  return padding;
 }
 private static double PositiveDimension(double? first,double? second,double fallback)=>first is >0&&double.IsFinite(first.Value)?first.Value:second is >0&&double.IsFinite(second.Value)?second.Value:fallback;
 public static byte[] BuildEscPosBytes(string receipt,double width,bool epsonU220)
 {
  if(!PaperWidths.Contains(width))throw new ArgumentException("Unsupported receipt paper width.",nameof(width));
  // Epson's default font B prints 40 / 36 / 30 columns at its supported widths.
  var columns=epsonU220?width<=58?30:width==69.5?36:40:width<=58?32:42;
  var text=receipt.Replace("₱","PHP ").Replace("\r","").Replace("×","x").Replace("−","-").Replace("–","-").Replace("—","-").Replace("·"," ");
  var lines=new List<string>();foreach(var line in text.Split('\n')){if(line.Length==0){lines.Add("");continue;}for(var offset=0;offset<line.Length;offset+=columns)lines.Add(line.Substring(offset,Math.Min(columns,line.Length-offset)));}
  Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
  return new byte[]{27,64,27,77,1,27,116,2}.Concat(Encoding.GetEncoding(850).GetBytes(string.Join("\n",lines)+"\n\n\n")).Concat(new byte[]{29,86,0}).ToArray();
 }
}
