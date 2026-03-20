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
    private readonly ExcelExportService _excelExportService;

    /// <summary>
    /// Constructor for FileService that initializes the service with a transaction repository, category repository, and categorizer service.
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="categorizer"></param>

    public FileService(ITransacaoRepository repository, CategorizerService categorizer, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categorizer = categorizer;
        _categoryRepository = categoryRepository;
        _excelExportService = new ExcelExportService(repository, categorizer, categoryRepository);
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
    
    public async Task<int> ProcessOfxToExcel(string folder, IXLWorksheet ws, ColumnMap col, HashSet<string> existingTransactions, int currentLine)
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

                        await _excelExportService.WriteTransactionLine(ws, currentLine, col, tx, cleanDesc, cultureInfo, defaultStyle);

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
    /// Processes a single OFX file, reading transactions and saving them to the database.
    /// The method reads the OFX file, extracts transactions, checks for duplicates against existing transactions in the database,
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>
    /// Returns the number of transactions added to the database after processing the specified OFX file.
    /// The method reads the OFX file, extracts transactions, checks for duplicates against existing transactions in the database,
    /// </returns>

    public async Task<int> ProcessSingleOfxToDb(string filePath)
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
            await WriteDbLine(tx);
            added++;
        }

        File.Delete(tempFile);
        return added;
    }


    public async Task<int> ProcessOfxToDb(string folder)
    {
        int added = 0;
        var files = Directory.GetFiles(folder, "*.ofx");
        var cultureInfo = new CultureInfo("pt-BR");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding1252 = Encoding.GetEncoding(1252);

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
                        await WriteDbLine(tx);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar arquivo {file}: {ex.Message}");
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
    /// Writes a transaction to the database after categorizing it.
    /// The method takes an OFX transaction, cleans its description, identifies its category using the CategorizerService,
    /// and then creates a new Transaction object to be saved in the database.
    /// It checks for existing transactions to avoid duplicates before inserting the new transaction.
    /// </summary>
    /// <param name="tx"></param>
    /// <returns>
    /// Returns a Task representing the asynchronous operation of writing a transaction to the database. The method processes the given OFX transaction,
    /// categorizes it, checks for duplicates, and if it's unique, inserts it into the database.
    /// The return value indicates the completion of the database write operation.
    /// </returns>
    private async Task WriteDbLine(OfxSharp.Transaction tx)
    {
        string cleanDesc = Categorizer.CleanText(string.IsNullOrEmpty(tx.Name) ? tx.Memo : tx.Name);
        int categoryId = _categorizer.Identify(cleanDesc, tx.Amount);

        var transaction = new Models.Transaction
        {
            Date = tx.Date,
            Value = tx.Amount,
            Description = cleanDesc,
            CategoryId = categoryId,
            Type = tx.Amount < 0 ? "EXPENSE" : "INCOME",
            OriginalName = tx.Name
        };

        bool exists = await _repository.ExistsTransactionAsync(
            transaction.Date,
            transaction.Value,
            transaction.Description
        );

        if (!exists)
        {
            await _repository.InsertAsync(transaction);
        }
    }

}