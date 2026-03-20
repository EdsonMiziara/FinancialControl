using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.Shared.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FinancialControl.AzureFunction;

public class MonthlyAutomationFunction
{
    private readonly ILogger _logger;
    private readonly FileService _fileService; 

    public MonthlyAutomationFunction(ILoggerFactory loggerFactory, FileService fileService)
    {
        _logger = loggerFactory.CreateLogger<MonthlyAutomationFunction>();
        _fileService = fileService;
    }

    [Function("MonthlyFinancialSync")]
    public async Task Run([TimerTrigger("0 0 0 1 * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"Automation triggered at: {DateTime.Now}");

        string driveOfxFolderId = Environment.GetEnvironmentVariable("DRIVE_OFX_FOLDER_ID");
        string driveExcelFileId = Environment.GetEnvironmentVariable("DRIVE_EXCEL_FILE_ID");
        string googleCredentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");

        string tempPath = Path.GetTempPath();
        string tempOfxFolder = Path.Combine(tempPath, "ofx_imports");
        string localExcelPath = Path.Combine(tempPath, "finance_control_temp.xlsx");

        try
        {
            var driveService = new GoogleDriveService(googleCredentialsJson);

            _logger.LogInformation("Baixando arquivos do drive");
            await driveService.DownloadFileByIdAsync(driveExcelFileId, localExcelPath);
            await driveService.DownloadOfxFilesFromFolderAsync(driveOfxFolderId, tempOfxFolder);

            using var excelService = new SpreadSheetService(localExcelPath, false, new ColumnMap());
            var ws = excelService.ObtainSpreadsheet();

            var columns = FileService.ColumnMapping(ws);
            var existingTransactions = FileService.LoadExistentTransactions(ws, columns);

            int currentRow = ws.LastRowUsed()?.RowNumber() + 1 ?? columns.HeaderLine + 1;

            int addedCount = await _fileService.ProcessOfxFile(tempOfxFolder, ws, columns, existingTransactions, currentRow);

            if (addedCount > 0)
            {
                excelService.Save(currentRow - 1);
                await driveService.UpdateFileAsync(localExcelPath, driveExcelFileId);
                _logger.LogInformation($"Sucesso! {addedCount} novas transações foram syncadas com o cloud.");
            }
            else
            {
                _logger.LogInformation("Não foram encontradas novas tranções para adicionar.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Falha Crítica: {ex.Message}");
        }
        finally
        {
            if (File.Exists(localExcelPath)) File.Delete(localExcelPath);
            if (Directory.Exists(tempOfxFolder)) Directory.Delete(tempOfxFolder, true);
        }
    }
}