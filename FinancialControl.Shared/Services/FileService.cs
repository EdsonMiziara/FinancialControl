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
    private readonly ICategoryRepository _categoryRepository;

    private readonly CategorizerService _categorizer;

    /// <summary>
    /// Constructor for FileService that initializes the service with a transaction repository, category repository, and categorizer service.
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="categorizer"></param>

    public FileService(ITransacaoRepository repository, CategorizerService categorizer)
    {
        _repository = repository;
        _categorizer = categorizer;
    }

    /// <summary>
    /// Processes of OFX files in a folder, reading transactions and writing to an Excel worksheet. It checks for existing transactions to avoid duplicates and uses the CategorizerService
    /// to identify categories for each transaction. The method returns the number of transactions added to the worksheet.
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="ws"></param>
    /// <param name="col"></param>
    /// <param name="existingTransactions"></param>
    /// <param name="currentLine"></param>
    /// <returns>
    /// Returns the number of transactions added to the worksheet after processing all OFX files in the specified folder. The method reads each OFX file, extracts transactions, checks for duplicates against existing transactions,
    /// categorizes them, and writes new transactions to the Excel worksheet while also saving them to the database.
    /// </returns>
    
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

    /// <summary>
    /// Writes a transaction line to the Excel worksheet and saves it to the database.
    /// This method takes care of categorizing the transaction using the CategorizerService,
    /// writing the transaction details to the specified columns in the worksheet,
    /// </summary>
    /// <param name="ws"></param>
    /// <param name="line"></param>
    /// <param name="col"></param>
    /// <param name="tx"></param>
    /// <param name="desc"></param>
    /// <param name="culture"></param>
    /// <param name="style"></param>
    /// <returns>
    /// Returns a Task that represents the asynchronous operation of writing a transaction line to the Excel worksheet and saving it to the database.
    /// The method does not return any value upon completion,
    /// but it performs the necessary operations to update the worksheet and persist the transaction data in the database.
    /// </returns>
    
    public async Task WriteTransactionLine(IXLWorksheet ws, int line, ColumnMap col, OfxSharp.Transaction tx, string desc, CultureInfo culture, IXLStyle style)
    {
        int categoryId = _categorizer.Identify(desc, tx.Amount);

        var categoriesDict = await _categoryRepository.GetCategoriesAsync();
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


        var transacao = new Models.Transaction
        {
            Date = tx.Date,
            Value = tx.Amount,
            Description = desc,
            CategoryId = categoryId,
            Tipe = tx.Amount < 0 ? "EXPENSE" : "INCOME",
            OriginalName = tx.Name
        };

        await _repository.InsertAsync(transacao);
    }

    /// <summary>
    /// Loads existing transactions from the Excel worksheet to avoid duplicates when processing new OFX files.
    /// It reads the transaction details from the specified columns and creates
    /// a hash set of unique transaction identifiers based on the date, description, and value.
    /// </summary>
    /// <param name="ws"></param>
    /// <param name="col"></param>
    /// <returns>
    /// Returns a HashSet<string> containing unique identifiers for existing transactions in the Excel worksheet.
    /// Each identifier is a combination of the transaction date, cleaned description, and value formatted as "yyyy-MM-dd|description|value".
    /// </returns>
    
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

    /// <summary>
    /// Processes a single OFX file, reading transactions and saving them to the database.
    /// The method reads the OFX file, extracts transactions, checks for duplicates against existing transactions in the database,
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>
    /// Returns the number of transactions added to the database after processing the specified OFX file.
    /// The method reads the OFX file, extracts transactions, checks for duplicates against existing transactions in the database,
    /// </returns>
    
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

            var transacao = new Models.Transaction
            {
                Date = tx.Date,
                Value = tx.Amount,
                Description = desc,
                CategoryId = categoryId,
                Tipe = tx.Amount < 0 ? "EXPENSE" : "INCOME",
                OriginalName = tx.Name
            };

            bool existe = await _repository.ExistsTransactionAsync(
                transacao.Date,
                transacao.Value,
                transacao.Description
            );

            if (existe)
                continue;

            await _repository.InsertAsync(transacao);
            added++;
        }

        File.Delete(tempFile);
        return added;
    }

    /// <summary>
    /// Maps the columns of the Excel worksheet based on the header found. It searches for the "DESCRIÇÃO"
    /// header to determine the line number of the headers and then identifies the column numbers for
    /// Date, Month, Description, Value, Year, Type, and Category based on their respective headers.
    /// If a header is not found, it assigns default column numbers.
    /// </summary>
    /// <param name="ws"></param>
    /// <returns>
    /// Returns a ColumnMap object that contains the mapping of column numbers for
    /// Date, Month, Description, Value, Year, Type, and Category based on the headers found in the Excel worksheet.
    /// </returns>
    
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