using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Roselie.App;
using Xunit;

namespace Roselie.Tests;

public sealed class ReportExporterTests : IDisposable
{
    private readonly string _root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Roselie.ExportTests"));
    private readonly string _directory;
    public ReportExporterTests()
    {
        _directory = Path.GetFullPath(Path.Combine(_root, Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(_directory);
    }

    private string PathFor(string extension) => Path.Combine(_directory, "report." + extension);

    [Fact]
    public void CsvEscapesContentAndNeutralizesSpreadsheetFormulaText()
    {
        var path = PathFor("csv");
        var report = new ReportData("Export", ["Text", "Amount"],
        [
            ["=HYPERLINK(\"https://invalid.test\")", 1035.01m],
            ["  +SUM(A1:A2)", -10.05m], ["@SUM(A1)", 0.01m], ["-formula", 100m],
            ["Line, with \"quotes\"\nand a newline", 1.23m], ["000123", 99m]
        ]);
        ReportExporter.Export(report, path);
        var rows = ParseCsv(File.ReadAllText(path));
        Assert.Equal(7, rows.Count);
        Assert.Equal("'=HYPERLINK(\"https://invalid.test\")", rows[1][0]);
        Assert.Equal("'  +SUM(A1:A2)", rows[2][0]);
        Assert.Equal("'@SUM(A1)", rows[3][0]);
        Assert.Equal("'-formula", rows[4][0]);
        Assert.Equal("-10.05", rows[2][1]);
        Assert.Equal("Line, with \"quotes\"\nand a newline", rows[5][0]);
        Assert.Equal("000123", rows[6][0]);
        Assert.Equal("1035.01", rows[1][1]);
    }

    [Fact]
    public void ExcelContainsValidXmlAndPreservesNumericMoneyAndReferenceText()
    {
        var path = PathFor("xlsx");
        ReportExporter.Export(new ReportData("Export", ["Reference", "Amount", "Notes"],
            [["000123", 1035.01m, "=SUM(A1:A2)"], ["REF&<>", -0.01m, "Hair \"color\""]]), path);
        using var zip = ZipFile.OpenRead(path);
        foreach (var entry in zip.Entries.Where(x => x.FullName.EndsWith(".xml") || x.FullName.EndsWith(".rels")))
        {
            using var stream = entry.Open();
            Assert.NotNull(XDocument.Load(stream).Root);
        }
        var sheet = LoadXml(zip, "xl/worksheets/sheet1.xml");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = sheet.Descendants(ns + "row").ToArray();
        Assert.Equal(3, rows.Length);
        var cells = rows[1].Elements(ns + "c").ToArray();
        Assert.Equal("inlineStr", cells[0].Attribute("t")?.Value);
        Assert.Equal("000123", cells[0].Descendants(ns + "t").Single().Value);
        Assert.Null(cells[1].Attribute("t"));
        Assert.Equal("2", cells[1].Attribute("s")?.Value);
        Assert.Equal(1035.01m, decimal.Parse(cells[1].Element(ns + "v")!.Value, CultureInfo.InvariantCulture));
        Assert.Equal("inlineStr", cells[2].Attribute("t")?.Value);
        Assert.Equal("=SUM(A1:A2)", cells[2].Descendants(ns + "t").Single().Value);
        Assert.Empty(sheet.Descendants(ns + "f"));
        Assert.Equal("A1:C3", sheet.Descendants(ns + "autoFilter").Single().Attribute("ref")?.Value);
        var styles = LoadXml(zip, "xl/styles.xml");
        var formats = styles.Descendants(ns + "cellXfs").Single().Elements(ns + "xf").ToArray();
        var moneyFormatId = formats[2].Attribute("numFmtId")!.Value;
        if (moneyFormatId != "4")
        {
            var custom = styles.Descendants(ns + "numFmt").Single(x => x.Attribute("numFmtId")?.Value == moneyFormatId);
            Assert.Contains(".00", custom.Attribute("formatCode")!.Value);
        }
        var relationships = LoadXml(zip, "xl/_rels/workbook.xml.rels");
        Assert.Contains(relationships.Root!.Elements(), x => x.Attribute("Target")?.Value == "worksheets/sheet1.xml");
    }

    [Fact]
    public void ExcelOverwriteReplacesPriorArchiveAndSupportsColumnsBeyondZ()
    {
        var path = PathFor("xlsx");
        ReportExporter.Export(new ReportData("First", ["Old"], [["Previous data"]]), path);
        var columns = Enumerable.Range(1, 27).Select(i => "Column " + i).ToArray();
        ReportExporter.Export(new ReportData("Second", columns, [Enumerable.Range(1, 27).Cast<object?>().ToArray()]), path);
        using var zip = ZipFile.OpenRead(path);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = LoadXml(zip, "xl/worksheets/sheet1.xml");
        Assert.Equal(27, sheet.Descendants(ns + "row").First().Elements(ns + "c").Count());
        Assert.Equal("A1:AA2", sheet.Descendants(ns + "autoFilter").Single().Attribute("ref")?.Value);
        Assert.DoesNotContain("Previous data", sheet.ToString());
        Assert.Equal(zip.Entries.Count, zip.Entries.Select(x => x.FullName).Distinct().Count());
    }

    [Fact]
    public void PdfPageTreeCrossReferenceAndStreamLengthsMatchActualBytes()
    {
        var path = PathFor("pdf");
        var report = new ReportData("Financial (report)", ["Reference", "Amount"],
            Enumerable.Range(1, 85).Select(i => new object?[] { "Test (row) \\ " + i, i + 0.01m }).ToList(), "Test period");
        ReportExporter.Export(report, path);
        var bytes = File.ReadAllBytes(path);
        var pdf = Encoding.ASCII.GetString(bytes);
        Assert.StartsWith("%PDF-1.4\n", pdf);
        Assert.EndsWith("%%EOF", pdf);
        Assert.Contains("/Type /Pages /Count 3", pdf);
        Assert.Equal(3, Regex.Matches(pdf, @"/Type /Page\s").Count);
        Assert.Contains(@"Financial \(report\)", pdf);
        Assert.Contains(@"Test \(row\) \\", pdf);
        var xrefMatch = Regex.Match(pdf, @"startxref\n(\d+)\n%%EOF$");
        Assert.True(xrefMatch.Success);
        var xrefOffset = int.Parse(xrefMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.Equal("xref", pdf.Substring(xrefOffset, 4));
        var xrefLines = pdf[xrefOffset..].Split('\n');
        var objectCount = int.Parse(xrefLines[1].Split(' ')[1], CultureInfo.InvariantCulture) - 1;
        Assert.Equal(objectCount, Regex.Matches(pdf, @"(?m)^\d+ 0 obj$").Count);
        for (var id = 1; id <= objectCount; id++)
        {
            var offset = int.Parse(xrefLines[id + 2][..10], CultureInfo.InvariantCulture);
            Assert.True(pdf.AsSpan(offset).StartsWith($"{id} 0 obj\n", StringComparison.Ordinal));
        }
        var streams = Regex.Matches(pdf, @"<< /Length (\d+) >>\nstream\n");
        Assert.Equal(3, streams.Count);
        foreach (Match stream in streams)
        {
            var dataStart = stream.Index + stream.Length;
            var length = int.Parse(stream.Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.True(pdf.AsSpan(dataStart + length).StartsWith("\nendstream", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void EmptyPdfProducesAValidPageAndAnEmptyState()
    {
        var path = PathFor("pdf");
        ReportExporter.Export(new ReportData("Empty report", ["Name", "Amount"], []), path);
        var pdf = Encoding.ASCII.GetString(File.ReadAllBytes(path));
        Assert.Contains("/Type /Pages /Count 1", pdf);
        Assert.Contains("No records in the selected period.", pdf);
    }

    [Fact]
    public void UnsupportedExportExtensionIsRejectedBeforeCreatingAFile()
    {
        var path = PathFor("exe");
        Assert.Throws<ArgumentException>(() => ReportExporter.Export(new ReportData("Test", ["Name"], []), path));
        Assert.False(File.Exists(path));
    }

    private static XDocument LoadXml(ZipArchive zip, string path)
    {
        using var stream = zip.GetEntry(path)!.Open();
        return XDocument.Load(stream);
    }

    private static List<string[]> ParseCsv(string value)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            if (character == '\uFEFF' && i == 0) continue;
            if (character == '"')
            {
                if (quoted && i + 1 < value.Length && value[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { fields.Add(field.ToString()); field.Clear(); }
            else if (character is '\r' or '\n' && !quoted)
            {
                if (character == '\r' && i + 1 < value.Length && value[i + 1] == '\n') i++;
                fields.Add(field.ToString()); field.Clear(); rows.Add(fields.ToArray()); fields.Clear();
            }
            else field.Append(character);
        }
        if (field.Length != 0 || fields.Count != 0) { fields.Add(field.ToString()); rows.Add(fields.ToArray()); }
        return rows;
    }

    public void Dispose()
    {
        if (_directory.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            Directory.Delete(_directory, recursive: true);
    }
}
