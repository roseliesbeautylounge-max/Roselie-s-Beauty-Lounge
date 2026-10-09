using System.Text;
using Roselie.App;
using Xunit;

namespace Roselie.Tests;

public sealed class PrinterSupportTests
{
 [Fact] public void AutomaticSelectionPrefersSingleEpsonWithoutSelectingVirtualPrinter()
 {
  var pdf=new ReceiptPrinter("Microsoft Print to PDF","Microsoft",true,true,"Ready");var epson=new ReceiptPrinter("EPSON TM-U220 Receipt","APD TM-U220",false,true,"Ready");
  Assert.Equal(epson,ReceiptPrinterSupport.Select("",[pdf,epson]));Assert.Null(ReceiptPrinterSupport.Select("",[pdf]));Assert.Null(ReceiptPrinterSupport.Select("Missing Epson",[pdf,epson]));
 }
 [Fact] public void MultipleReceiptPrintersRequireDefaultOrExplicitSelection()
 {
  var one=new ReceiptPrinter("EPSON TM-U220 A","",false,true,"Ready");var two=new ReceiptPrinter("EPSON TM-U220 B","",false,true,"Ready");Assert.Null(ReceiptPrinterSupport.Select("",[one,two]));Assert.Equal(two,ReceiptPrinterSupport.Select(two.Name,[one,two]));
 }
 [Theory][InlineData("80",true,76)][InlineData("57.5",false,57.5)][InlineData("58",false,57.5)][InlineData("69.5",false,69.5)][InlineData("76",false,76)]
 public void EpsonProfileUsesSupportedWidths(string saved,bool automatic,double expected)=>Assert.Equal(expected,ReceiptPrinterSupport.ResolveWidth(saved,automatic,"EPSON TM-U220B"));
 [Fact] public void EpsonManual80mmIsRejected()=>Assert.Throws<InvalidOperationException>(()=>ReceiptPrinterSupport.ResolveWidth("80",false,"EPSON TM-U220B"));
 [Theory][InlineData(57.5,30)][InlineData(69.5,36)][InlineData(76,40)]
 public void RawEpsonTextWrapsInsideDefaultFontWidthAndRetainsSavedAmounts(double width,int columns)
 {
  var text=new string('A',95)+"\nTOTAL: PHP 1,399.00\nReceived: PHP 1,500.00\nChange: PHP 101.00";var bytes=ReceiptPrinterSupport.BuildEscPosBytes(text,width,true);Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);var payload=Encoding.GetEncoding(850).GetString(bytes[8..^3]);
  Assert.All(payload.Split('\n'),line=>Assert.True(line.Length<=columns));Assert.Contains("TOTAL: PHP 1,399.00",payload);Assert.Contains("Change: PHP 101.00",payload);Assert.Equal(95,payload.Count(c=>c=='A'));Assert.Equal(new byte[]{27,64,27,77,1,27,116,2},bytes[..8]);
 }
}
