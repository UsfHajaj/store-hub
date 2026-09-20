using System.Globalization;
using ClosedXML.Excel;

namespace StoreHub.Infrastructure.Excel;

internal sealed record ExcelSheetSpec(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<object?>>? Rows);

internal static class ExcelSheetHelper
{
    public static byte[] BuildWorkbook(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>>? rows,
        string? instructionsAr = null,
        string? instructionsEn = null) =>
        BuildMultiSheetWorkbook(
            new[] { new ExcelSheetSpec(sheetName, headers, rows) },
            instructionsAr,
            instructionsEn);

    public static byte[] BuildMultiSheetWorkbook(
        IReadOnlyList<ExcelSheetSpec> sheets,
        string? instructionsAr = null,
        string? instructionsEn = null)
    {
        using var wb = new XLWorkbook();
        foreach (var sheet in sheets)
        {
            var ws = wb.Worksheets.Add(sheet.Name);
            for (var c = 0; c < sheet.Headers.Count; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = sheet.Headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EC5B38");
                cell.Style.Font.FontColor = XLColor.White;
            }

            if (sheet.Rows is not null)
            {
                for (var r = 0; r < sheet.Rows.Count; r++)
                {
                    var row = sheet.Rows[r];
                    for (var c = 0; c < row.Count; c++)
                    {
                        SetCell(ws.Cell(r + 2, c + 1), row[c]);
                    }
                }
            }

            ws.Columns().AdjustToContents(1, 48);
        }

        if (!string.IsNullOrWhiteSpace(instructionsAr) || !string.IsNullOrWhiteSpace(instructionsEn))
        {
            var guide = wb.Worksheets.Add("Instructions");
            guide.Cell(1, 1).Value = "تعليمات / Instructions";
            guide.Cell(1, 1).Style.Font.Bold = true;
            guide.Cell(1, 1).Style.Font.FontSize = 14;
            guide.Cell(2, 1).Value = instructionsAr ?? string.Empty;
            guide.Cell(2, 1).Style.Alignment.WrapText = true;
            guide.Cell(4, 1).Value = instructionsEn ?? string.Empty;
            guide.Cell(4, 1).Style.Alignment.WrapText = true;
            guide.Column(1).Width = 100;
            guide.Row(2).Height = 160;
            guide.Row(4).Height = 160;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static IReadOnlyList<Dictionary<string, string>> ReadRows(Stream stream, string? sheetName = null)
    {
        using var wb = new XLWorkbook(stream);
        return ReadRows(wb, sheetName);
    }

    public static IReadOnlyList<Dictionary<string, string>> ReadRows(XLWorkbook wb, string? sheetName = null)
    {
        IXLWorksheet ws;
        if (!string.IsNullOrWhiteSpace(sheetName))
        {
            ws = wb.Worksheets.FirstOrDefault(w =>
                string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Sheet '{sheetName}' was not found.");
        }
        else
        {
            ws = wb.Worksheets.First(w =>
                !string.Equals(w.Name, "Instructions", StringComparison.OrdinalIgnoreCase));
        }

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        if (lastCol == 0 || lastRow < 2)
        {
            return Array.Empty<Dictionary<string, string>>();
        }

        var headers = new string[lastCol];
        for (var c = 1; c <= lastCol; c++)
        {
            headers[c - 1] = ws.Cell(1, c).GetString().Trim();
        }

        var list = new List<Dictionary<string, string>>();
        for (var r = 2; r <= lastRow; r++)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var any = false;
            for (var c = 1; c <= lastCol; c++)
            {
                var key = headers[c - 1];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var val = ws.Cell(r, c).GetFormattedString().Trim();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    any = true;
                }

                dict[key] = val;
            }

            if (any)
            {
                list.Add(dict);
            }
        }

        return list;
    }

    public static bool HasSheet(XLWorkbook wb, string sheetName) =>
        wb.Worksheets.Any(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));

    public static string Get(IReadOnlyDictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return string.Empty;
    }

    public static bool? ParseBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var v = raw.Trim().ToLowerInvariant();
        return v switch
        {
            "1" or "true" or "yes" or "y" or "نعم" or "ايوه" or "صح" => true,
            "0" or "false" or "no" or "n" or "لا" => false,
            _ => null
        };
    }

    public static decimal? ParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (decimal.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        if (decimal.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.GetCultureInfo("ar-EG"), out d))
        {
            return d;
        }

        return null;
    }

    private static void SetCell(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = Blank.Value;
                break;
            case string s:
                cell.Value = s;
                break;
            case int i:
                cell.Value = i;
                break;
            case decimal d:
                cell.Value = d;
                break;
            case double db:
                cell.Value = db;
                break;
            case bool b:
                cell.Value = b;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
