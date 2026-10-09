using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Roselie.App;

public sealed record ReportData(string Title,string[] Columns,List<object?[]> Rows,string Period="");

public static class ReportExporter
{
 public static void Export(ReportData report,string path)
 {
  switch(Path.GetExtension(path).ToLowerInvariant())
  {
   case ".csv":Csv(report,path);break;
   case ".xlsx":Excel(report,path);break;
   case ".pdf":Pdf(report,path);break;
   default:throw new ArgumentException("Choose a PDF, Excel or CSV file.");
  }
 }
 private static string Value(object? value)=>value switch{null=>"",decimal d=>d.ToString("0.######",CultureInfo.InvariantCulture),DateTime dt=>dt.ToString("yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture),_=>value.ToString()??""};
 private static void Csv(ReportData report,string path)
 {
  string Cell(object? value){var s=Value(value);if(value is string && s.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@')s="'"+s;return "\""+s.Replace("\"","\"\"")+"\"";}
  using var writer=new StreamWriter(path,false,new UTF8Encoding(true));writer.WriteLine(string.Join(",",report.Columns.Select(Cell)));foreach(var row in report.Rows)writer.WriteLine(string.Join(",",row.Select(Cell)));
 }
 private static void Excel(ReportData report,string path)
 {
  using var output=File.Create(path);
  using var archive=new ZipArchive(output,ZipArchiveMode.Create);
  void Entry(string name,string text){using var writer=new StreamWriter(archive.CreateEntry(name).Open(),new UTF8Encoding(false));writer.Write(text);}
  Entry("[Content_Types].xml","<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
  Entry("_rels/.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
  Entry("xl/workbook.xml","<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Report\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
  Entry("xl/_rels/workbook.xml.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
  Entry("xl/styles.xml","<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.00####\"/></numFmts><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><color rgb=\"FFA85E67\"/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf/><xf fontId=\"1\" applyFont=\"1\"/><xf numFmtId=\"164\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>");
  using var stream=archive.CreateEntry("xl/worksheets/sheet1.xml").Open();using var xml=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false)});const string ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
  xml.WriteStartElement("worksheet",ns);xml.WriteStartElement("cols",ns);xml.WriteStartElement("col",ns);xml.WriteAttributeString("min","1");xml.WriteAttributeString("max",report.Columns.Length.ToString());xml.WriteAttributeString("width","24");xml.WriteAttributeString("customWidth","1");xml.WriteEndElement();xml.WriteEndElement();xml.WriteStartElement("sheetData",ns);
  var rows=new List<object?[]>{report.Columns.Cast<object?>().ToArray()};rows.AddRange(report.Rows);
  for(var i=0;i<rows.Count;i++){xml.WriteStartElement("row",ns);xml.WriteAttributeString("r",(i+1).ToString());foreach(var value in rows[i]){xml.WriteStartElement("c",ns);xml.WriteAttributeString("s",i==0?"1":value is decimal?"2":"0");if(value is decimal or int or long){xml.WriteElementString("v",ns,Value(value));}else{xml.WriteAttributeString("t","inlineStr");xml.WriteStartElement("is",ns);xml.WriteElementString("t",ns,Value(value));xml.WriteEndElement();}xml.WriteEndElement();}xml.WriteEndElement();}xml.WriteEndElement();xml.WriteStartElement("autoFilter",ns);xml.WriteAttributeString("ref",$"A1:{ColumnName(report.Columns.Length)}{Math.Max(1,rows.Count)}");xml.WriteEndElement();xml.WriteEndElement();
 }
 private static string ColumnName(int number){var s="";while(number>0){number--;s=(char)('A'+number%26)+s;number/=26;}return s;}
 private static void Pdf(ReportData report,string path)
 {
  // Dependency-free, standards-based PDF. Peso amounts are labelled PHP;
  // basic Latin text is embedded with the built-in Courier font.
  string Safe(string value)=>new(value.Normalize(NormalizationForm.FormD).Where(c=>c<128 && c!='\r' && c!='\n' && !char.IsControl(c)).ToArray());
  string Escape(string value)=>Safe(value).Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)");
  var widths=report.Columns.Select((c,i)=>Math.Min(32,Math.Max(c.Length,report.Rows.Select(r=>i<r.Length?Value(r[i]).Length:0).DefaultIfEmpty(0).Max()))).ToArray();
  string Row(object?[] values)=>string.Join(" | ",values.Select((v,i)=>{var s=Value(v);return s.Length>widths[i]?s[..Math.Max(0,widths[i]-1)]+"~":s.PadRight(widths[i]);}));
  var lines=new List<string>{Row(report.Columns.Cast<object?>().ToArray()),new('-',Math.Min(150,widths.Sum()+3*(widths.Length-1)))};lines.AddRange(report.Rows.Select(Row));if(report.Rows.Count==0)lines.Add("No records in the selected period.");
  var chunks=lines.Chunk(42).ToArray();var objects=new List<byte[]>();byte[] Bytes(string s)=>Encoding.ASCII.GetBytes(s);objects.Add(Bytes("<< /Type /Catalog /Pages 2 0 R >>"));objects.Add([]);objects.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"));var pages=new List<int>();
  for(var p=0;p<chunks.Length;p++)
  {
   var pageId=objects.Count+1;var streamId=pageId+1;pages.Add(pageId);var landscape=widths.Sum()+3*widths.Length>85;var width=landscape?842:595;var height=landscape?595:842;var font=landscape?Math.Max(5.5,Math.Min(9.0,760.0/Math.Max(1,widths.Sum()+3*widths.Length)/0.6)):9.0;
   objects.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Resources << /Font << /F1 3 0 R >> >> /Contents {streamId} 0 R >>"));
   var content=new StringBuilder($"BT /F1 16 Tf 40 {height-42} Td (Roselie's Beauty Lounge) Tj /F1 12 Tf 0 -24 Td ({Escape(report.Title)}) Tj /F1 9 Tf 0 -18 Td ({Escape(report.Period)}) Tj /F1 {font.ToString("0.0",CultureInfo.InvariantCulture)} Tf 0 -25 Td ");
   foreach(var line in chunks[p])content.Append($"({Escape(line)}) Tj 0 -11 Td ");content.Append($"ET BT /F1 8 Tf 40 22 Td (Page {p+1} of {chunks.Length} | Amounts in PHP | Internal management report) Tj ET");var body=Bytes(content.ToString());objects.Add(Bytes($"<< /Length {body.Length} >>\nstream\n{content}\nendstream"));
  }
  objects[1]=Bytes($"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(" ",pages.Select(id=>$"{id} 0 R"))}] >>");using var file=File.Create(path);void Write(string s)=>file.Write(Bytes(s));Write("%PDF-1.4\n");var offsets=new List<long>{0};for(var i=0;i<objects.Count;i++){offsets.Add(file.Position);Write($"{i+1} 0 obj\n");file.Write(objects[i]);Write("\nendobj\n");}var xref=file.Position;Write($"xref\n0 {objects.Count+1}\n0000000000 65535 f \n");foreach(var offset in offsets.Skip(1))Write($"{offset:0000000000} 00000 n \n");Write($"trailer\n<< /Size {objects.Count+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
 }
}
