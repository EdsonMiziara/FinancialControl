using ClosedXML.Excel;
using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.Shared.Interfaces;
using FinancialControl.Shared.Models;
using OfxSharp;
using System.Globalization;
using System.Text;

namespace FinancialControl.Shared.Services;

public class FileService
{
    private readonly ITransacaoRepository _repository;

    private readonly CategorizerService _categorizer;

    public FileService(ITransacaoRepository repository, CategorizerService categorizer)
    {
        _repository = repository;
        _categorizer = categorizer;
    }

    // Alterado para async Task<int>
    public async Task<int> ProcessOfxFile(string folder, IXLWorksheet ws, ColumnMap col, HashSet<string> existingTransactions, int currentLine)
    {
        int added = 0;
        var files = Directory.GetFiles(folder, "*.ofx");
        var cultureInfo = new CultureInfo("pt-BR");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding1252 = Encoding.GetEncoding(1252);

        var defaultStyle = ws.Row(currentLine - 1).Style;

        foreach (var file in files)
        {
            string ofxContent = File.ReadAllText(file, encoding1252);
            string tempFilePath = file + ".tmp";

            try
            {
                File.WriteAllText(tempFilePath, ofxContent, Encoding.UTF8);

                using (var stream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                {
                    var document = new OFXDocumentParser().Import(stream);

                    foreach (var tx in document.Transactions)
                    {
                        string cleanDesc = Categorizer.CleanText(string.IsNullOrEmpty(tx.Name) ? tx.Memo : tx.Name);

                        if (existingTransactions.Contains($"{tx.Date:yyyy-MM-dd}|{cleanDesc}|{tx.Amount:F2}")) continue;

                        // Agora usamos o await aqui!
                        await WriteTransactionLine(ws, currentLine, col, tx, cleanDesc, cultureInfo, defaultStyle);

                        currentLine++;
                        added++;
                    }
                }
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        return added;
    }

    public async Task WriteTransactionLine(IXLWorksheet ws, int line, ColumnMap col, Transaction tx, string desc, CultureInfo culture, IXLStyle style)
    {
        int categoryId = _categorizer.Identify(desc, tx.Amount);

        var categoriesDict = await _repository.GetCategoriasAsync();
        string categoryNome = categoriesDict[categoryId];

        ws.Cell(line, col.Date).Value = tx.Date;
        ws.Cell(line, col.Month).Value = culture.DateTimeFormat.GetAbbreviatedMonthName(tx.Date.Month).ToLower();
        ws.Cell(line, col.Year).Value = tx.Date.Year;
        ws.Cell(line, col.Type).Value = tx.Amount < 0 ? "EXPENSE" : "INCOME";
        ws.Cell(line, col.Category).Value = categoryNome;
        ws.Cell(line, col.Description).Value = desc;
        ws.Cell(line, col.Value).Value = tx.Amount;

        var range = ws.Range(line, col.Date, line, col.Value);
        range.Style = style;

        ws.Cell(line, col.Date).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Cell(line, col.Value).Style.NumberFormat.Format = "R$ #,##0.00";


        var transacao = new Transacao
        {
            Data = tx.Date,
            Valor = tx.Amount,
            Descricao = desc,
            CategoriaId = categoryId,
            Tipo = tx.Amount < 0 ? "EXPENSE" : "INCOME",
            NomeOriginal = tx.Name
        };

        await _repository.InsertAsync(transacao);
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
    public async Task<int> ProcessSingleOfx(string filePath)
    {
        int added = 0;

        var encoding = Encoding.GetEncoding(1252);
        string content = File.ReadAllText(filePath, encoding);

        var tempFile = filePath + ".tmp";
        File.WriteAllText(tempFile, content, Encoding.UTF8);

        using var stream = new FileStream(tempFile, FileMode.Open);
        var document = new OFXDocumentParser().Import(stream);

        foreach (var tx in document.Transactions)
        {
            string desc = Categorizer.CleanText(tx.Name ?? tx.Memo);
            int categoryId = _categorizer.Identify(desc, tx.Amount);

            var transacao = new Transacao
            {
                Data = tx.Date,
                Valor = tx.Amount,
                Descricao = desc,
                CategoriaId = categoryId,
                Tipo = tx.Amount < 0 ? "EXPENSE" : "INCOME",
                NomeOriginal = tx.Name
            };

            bool existe = await _repository.ExistsAsync(
                transacao.Data,
                transacao.Valor,
                transacao.Descricao
            );

            if (existe)
                continue;

            await _repository.InsertAsync(transacao);
            added++;
        }

        File.Delete(tempFile);
        return added;
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