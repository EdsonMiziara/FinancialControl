using ClosedXML.Excel;
using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.Shared.Interfaces;
using System.Globalization;

namespace FinancialControl.Shared.Services;

public class ExcelExportService
{
    private readonly ITransacaoRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategorizerService _categorizer;
    public ExcelExportService( ITransacaoRepository repository, CategorizerService categorizer, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categorizer = categorizer;
        _categoryRepository = categoryRepository;
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

        if (!categoriesDict.TryGetValue(categoryId, out string categoryName))
            categoryName = "Extra";

        ws.Cell(line, col.Date).Value = tx.Date;
        ws.Cell(line, col.Month).Value = culture.DateTimeFormat.GetAbbreviatedMonthName(tx.Date.Month).ToLower();
        ws.Cell(line, col.Year).Value = tx.Date.Year;
        ws.Cell(line, col.Type).Value = tx.Amount < 0 ? "EXPENSE" : "INCOME";
        ws.Cell(line, col.Category).Value = categoryName;
        ws.Cell(line, col.Description).Value = desc;
        ws.Cell(line, col.Value).Value = tx.Amount;

        var range = ws.Range(line, col.Date, line, col.Value);
        range.Style = style;

        ws.Cell(line, col.Date).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Cell(line, col.Value).Style.NumberFormat.Format = "R$ #,##0.00";
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
        int headerLine = 2;
        var row = ws.Row(headerLine);

        int GetColumn(string keyword)
        {
            var cell = row.Cells()
                .FirstOrDefault(c =>
                    !c.IsEmpty() &&
                    c.GetString().Trim().ToUpper().Contains(keyword));

            if (cell == null)
                throw new Exception($"Column '{keyword}' not found.");

            return cell.Address.ColumnNumber;
        }

        return new ColumnMap
        {
            HeaderLine = headerLine,
            Date = GetColumn("DATA"),
            Month = GetColumn("MÊS"),
            Year = GetColumn("ANO"),
            Type = GetColumn("TIPO"),
            Category = GetColumn("CATEGORIA"),
            Description = GetColumn("DESCRIÇÃO"),
            Value = GetColumn("VALOR")
        };
    }
    public static void Validate(ColumnMap col)
    {
        var props = typeof(ColumnMap).GetProperties();

        foreach (var p in props)
        {
            if (p.PropertyType == typeof(int))
            {
                int value = (int)p.GetValue(col);
                if (value <= 0)
                    throw new Exception($"Invalid column: {p.Name} = {value}");
            }
        }
    }
}
