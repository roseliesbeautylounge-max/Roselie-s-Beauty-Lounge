using System.Windows;
using Roselie.App;
using Xunit;

namespace Roselie.Tests;

public sealed class PrinterPageLayoutTests
{
 [Fact]
 public void EpsonPaperMarginsRemainWhenDriverAllowsThem()
 {
  var paper=new Size(76/25.4*96,1000);
  var explicitMargins=new Thickness(6.8/25.4*96,5,5.8/25.4*96,5);
  Assert.Equal(explicitMargins,ReceiptPrinterSupport.BoundMargins(explicitMargins,paper,new Rect(new Point(),paper)));
 }
 [Fact]
 public void EveryContentEdgeStaysInsideTheDriverPrintableArea()
 {
  var paper=new Size(300,1000);var printable=new Rect(18,24,270,940);
  var margins=ReceiptPrinterSupport.BoundMargins(new Thickness(5),paper,printable);
  var content=new Rect(margins.Left,margins.Top,paper.Width-margins.Left-margins.Right,paper.Height-margins.Top-margins.Bottom);
  Assert.True(printable.Contains(content));Assert.Equal(new Thickness(18,24,12,36),margins);
 }
 [Fact]
 public void NarrowReceiptOnLargerVirtualPageKeepsItsOriginalRightMargin()
 {
  var margins=ReceiptPrinterSupport.BoundMargins(new Thickness(5),new Size(302,1056),new Rect(24,24,740,1000));
  Assert.Equal(24,margins.Left);Assert.Equal(5,margins.Right);Assert.Equal(24,margins.Top);Assert.Equal(32,margins.Bottom);
 }
 [Fact]
 public void DriverMarginsThatLeaveNoRoomFailBeforeSpooling()
 {
  Assert.Throws<InvalidOperationException>(()=>ReceiptPrinterSupport.BoundMargins(new Thickness(5),new Size(230,600),new Rect(220,0,10,600)));
 }
 [Fact]
 public void InvalidDriverPaperDimensionsFailBeforeSpooling()
 {
  Assert.Throws<InvalidOperationException>(()=>ReceiptPrinterSupport.BoundMargins(new Thickness(5),new Size(double.PositiveInfinity,600),new Rect(0,0,230,600)));
 }
}
