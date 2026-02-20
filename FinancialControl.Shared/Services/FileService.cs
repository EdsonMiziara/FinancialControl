using ClosedXML.Excel;
using FinancialControl.ConsoleApp.SupportModels;
using OfxSharp;
using System.Globalization;

namespace FinancialControl.Shared.Services;

public static class FileService
{

    public static int ProcessOfxFile(string folder, IXLWorksheet ws, ColumnMap col, HashSet<string> existents, ref int actualLine)
    {
        int added = 0;
        var files = Directory.GetFiles(folder, "*.ofx");
        var BRCulture = new CultureInfo("pt-BR");

        // Pega estilo da linha de cabeçalho se for a primeira inserção, ou da última linha preenchida
        var defaultStyle = ws.Row(actualLine - 1).Style;

        foreach (var file in files)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            var document = new OFXDocumentParser().Import(stream);

            foreach (var tx in document.Transactions)
            {
                string cleanDesc = Categorizer.RemoveAccents(tx.Memo);
                if (existents.Contains($"{tx.Date:yyyy-MM-dd}|{cleanDesc}|{tx.Amount:F2}")) continue;

                WriteTransactionLine(ws, actualLine, col, tx, cleanDesc, BRCulture, defaultStyle);

                actualLine++;
                added++;
            }
        }
        return added;
    }
    public static void WriteTransactionLine(IXLWorksheet ws, int line, ColumnMap col, Transaction tx, string desc, CultureInfo culture, IXLStyle style)
    {
        ws.Cell(line, col.Date).Value = tx.Date;
        ws.Cell(line, col.Month).Value = culture.DateTimeFormat.GetAbbreviatedMonthName(tx.Date.Month).ToLower();
        ws.Cell(line, col.Year).Value = tx.Date.Year;
        ws.Cell(line, col.Type).Value = tx.Amount < 0 ? "DESPESA" : "RECEITA";
        ws.Cell(line, col.Category).Value = Categorizer.Identify(tx.Memo, tx.Amount);
        ws.Cell(line, col.Description).Value = desc;
        ws.Cell(line, col.Value).Value = tx.Amount;

        var range = ws.Range(line, col.Date, line, col.Value);
        range.Style = style;

        ws.Cell(line, col.Date).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Cell(line, col.Value).Style.NumberFormat.Format = "R$ #,##0.00";
        ws.Cell(line, col.Year).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        // Cores baseadas no valor (conforme solicitado anteriormente)
        if (tx.Amount < 0)
        {
            ws.Cell(line, col.Value).Style.Fill.BackgroundColor = XLColor.LightPink;
            ws.Cell(line, col.Value).Style.Font.FontColor = XLColor.DarkRed;
        }
        else
        {
            ws.Cell(line, col.Value).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3"); // Verde claro da imagem
            ws.Cell(line, col.Value).Style.Font.FontColor = XLColor.DarkGreen;
        }
    }

    public static HashSet<string> LoadExistentTransactions(IXLWorksheet ws, ColumnMap col)
    {
        var hash = new HashSet<string>();
        int last = ws.LastRowUsed()?.RowNumber() ?? col.HeaderLine;

        for (int i = col.HeaderLine + 1; i <= last; i++)
        {
            string date = ws.Cell(i, col.Date).TryGetValue<DateTime>(out var d) ? d.ToString("yyyy-MM-dd") : "";
            string desc = Categorizer.CleanText(ws.Cell(i, col.Description).GetString());
            string value = ws.Cell(i, col.Value).TryGetValue<decimal>(out var v) ? v.ToString("F2") : "";
            hash.Add($"{date}|{desc}|{value}");
        }
        return hash;
    }

    public static ColumnMap ColumnMapping(IXLWorksheet ws)
    {
        var header = ws.Search("DESCRIÇÃO").FirstOrDefault();
        int line = header?.Address.RowNumber ?? 1;
        var r = ws.Row(line);

        return new ColumnMap
        {
            HeaderLine = line,
            Date = r.CellsUsed().FirstOrDefault(c => c.Value.ToString().ToUpper().Contains("DATA"))?.Address.ColumnNumber ?? 2,
            Month = r.CellsUsed().FirstOrDefault(c => c.Value.ToString().ToUpper().Contains("MÊS"))?.Address.ColumnNumber ?? 3,
            Description = header?.Address.ColumnNumber ?? 7,
            Value = r.CellsUsed().FirstOrDefault(c => c.Value.ToString().ToUpper().Contains("VALOR"))?.Address.ColumnNumber ?? 8,
            Year = 4,
            Type = 5,
            Category = 6
        };
    }

}
