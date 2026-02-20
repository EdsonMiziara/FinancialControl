using ClosedXML.Excel;
using FinancialControl.ConsoleApp.SupportModels;

public class SpreadSheetService : IDisposable
{
    private readonly XLWorkbook _workbook;
    private readonly IXLWorksheet _ws;
    private readonly string path;

    public SpreadSheetService(string path, bool isNew, ColumnMap col)
    {
        this.path = path;
        _workbook = isNew ? new XLWorkbook() : new XLWorkbook(path);

        // Se for novo, adiciona a planilha; se existir, busca ou cria a aba
        if (isNew)
        {
            _ws = _workbook.Worksheets.Add("CONTROLE");
            CreateDefaultStructure(_ws, col);
        }
        else
        {
            _ws = _workbook.Worksheets.Contains("CONTROLE")
                  ? _workbook.Worksheet("CONTROLE")
                  : _workbook.Worksheets.Add("CONTROLE");
        }
    }

    private void CreateDefaultStructure(IXLWorksheet ws, ColumnMap col)
    {
        ws.Cell(col.HeaderLine, col.Date).Value = "DATA";
        ws.Cell(col.HeaderLine, col.Month).Value = "MÊS";
        ws.Cell(col.HeaderLine, col.Year).Value = "ANO";
        ws.Cell(col.HeaderLine, col.Type).Value = "TIPO";
        ws.Cell(col.HeaderLine, col.Category).Value = "CATEGORIA";
        ws.Cell(col.HeaderLine, col.Description).Value = "DESCRIÇÃO";
        ws.Cell(col.HeaderLine, col.Value).Value = "VALOR";

        // Opcional: Criar a tabela oficial do Excel imediatamente
        var range = ws.Range(col.HeaderLine, col.Date, col.HeaderLine, col.Value);
        range.CreateTable();
    }

    public IXLWorksheet ObtainSpreadsheet() => _ws;

    public void Save(int finalLine)
    {
        TableAdjust(finalLine);
        _workbook.SaveAs(path);
    }

    private void TableAdjust(int finalLine)
    {
        var table = _ws.Tables.FirstOrDefault();
        if (table != null && finalLine > table.LastRowUsed().RowNumber())
        {
            table.Resize(_ws.Range(
                table.FirstCell().Address.RowNumber,
                table.FirstCell().Address.ColumnNumber,
                finalLine,
                table.LastCell().Address.ColumnNumber
            ));
        }
    }

    public void Dispose() => _workbook.Dispose();
}