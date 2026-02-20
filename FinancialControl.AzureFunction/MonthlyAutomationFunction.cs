using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.Shared.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FinancialControl.AzureFunction;

public class MonthlyAutomationFunction
{
    private readonly ILogger _logger;

    public MonthlyAutomationFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MonthlyAutomationFunction>();
    }

    [Function("MonthlyFinancialSync")]
    public async Task Run([TimerTrigger("0 0 0 1 * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"Automation triggered at: {DateTime.Now}");

        // Environment Variables in English (Configured in Azure Portal)
        string driveOfxFolderId = Environment.GetEnvironmentVariable("DRIVE_OFX_FOLDER_ID");
        string driveExcelFileId = Environment.GetEnvironmentVariable("DRIVE_EXCEL_FILE_ID");
        string googleCredentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");

        // Azure Temporary Storage
        string tempPath = Path.GetTempPath();
        string tempOfxFolder = Path.Combine(tempPath, "ofx_imports");
        string localExcelPath = Path.Combine(tempPath, "finance_control_temp.xlsx");

        try
        {
            var driveService = new GoogleDriveService(googleCredentialsJson);

            // 1. Download Phase
            _logger.LogInformation("Baixando arquivos do drive");
            await driveService.DownloadFileByIdAsync(driveExcelFileId, localExcelPath);
            await driveService.DownloadOfxFilesFromFolderAsync(driveOfxFolderId, tempOfxFolder);

            // 2. Processing Phase
            using var excelService = new SpreadSheetService(localExcelPath, false, new ColumnMap());
            var ws = excelService.ObtainSpreadsheet();

            var columns = FileService.ColumnMapping(ws);
            var existingTransactions = FileService.LoadExistentTransactions(ws, columns);

            int currentRow = ws.LastRowUsed()?.RowNumber() + 1 ?? columns.HeaderLine + 1;

            // Process local files downloaded from Drive
            int addedCount = FileService.ProcessOfxFile(tempOfxFolder, ws, columns, existingTransactions, ref currentRow);

            // 3. Upload Phase (Sync back to Drive if changed)
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
            // Here you could integrate SendGrid or Azure Monitor for alerts
        }
        finally
        {
            // Cleanup to free Azure resources
            if (File.Exists(localExcelPath)) File.Delete(localExcelPath);
            if (Directory.Exists(tempOfxFolder)) Directory.Delete(tempOfxFolder, true);
        }
    }
}