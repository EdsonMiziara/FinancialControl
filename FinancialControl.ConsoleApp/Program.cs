using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.Shared.Services;
using FinancialControl.Shared.SupportModels;
using FinancialControl.Shared.SupportModels.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        RunAsync().GetAwaiter().GetResult();
    }

    static async Task RunAsync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;

        var dbContext = new AppDbContext(options);

        var categoryRepository = new CategoryRepository(configuration);
        var transactionRepository = new TransactionRepository(configuration, categoryRepository);
        var loader = new CategorizerLoader(dbContext);
        var cache = await loader.LoadAsync();
        var categorizer = new CategorizerService(cache, dbContext);

        var fileService = new FileService(transactionRepository, categorizer, categoryRepository);

        var spreadsheetChoice = MessageBox.Show(
            "Do you want to use an existing spreadsheet?\n\n(Yes = Select Existing | No = Create New)",
            "Configuration",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (spreadsheetChoice == DialogResult.Cancel)
            return;

        bool isNewSpreadsheet = spreadsheetChoice == DialogResult.No;

        if (!TryGetPaths(isNewSpreadsheet, out string ofxFolderPath, out string excelFilePath))
            return;

        if (!Directory.Exists(ofxFolderPath))
        {
            MessageBox.Show("Selected OFX folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!isNewSpreadsheet && !File.Exists(excelFilePath))
        {
            MessageBox.Show("Selected Excel file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var columnMap = new ColumnMap();
            using var spreadsheetService = new SpreadSheetService(excelFilePath, isNewSpreadsheet, columnMap);
            var worksheet = spreadsheetService.ObtainSpreadsheet();

            var columns = isNewSpreadsheet ? columnMap : ExcelExportService.ColumnMapping(worksheet);
            var existingTransactions = isNewSpreadsheet ? new HashSet<string>() : ExcelExportService.LoadExistentTransactions(worksheet, columns);

            int currentRow = worksheet.LastRowUsed()?.RowNumber() + 1 ?? columns.HeaderLine + 1;

            int totalAddedExcel = await fileService.ProcessOfxToExcel(ofxFolderPath, worksheet, columns, existingTransactions, currentRow);
            int totalAddedDb = await fileService.ProcessOfxToDb(ofxFolderPath);

            if (totalAddedExcel > 0)
            {
                spreadsheetService.Save(currentRow - 1);
                MessageBox.Show($"Success! {totalAddedExcel} new transactions added to excel.", "Completed");

            }
            if (totalAddedDb > 0)
            {
                MessageBox.Show($"Success! {totalAddedDb} new transactions added to excel.", "Completed");
            }
            if (totalAddedDb <= 0)
            {
                MessageBox.Show($"Warning: No transactions was added to database");
            }
            if (totalAddedExcel <= 0)
            {

                MessageBox.Show($"Warning: No transactions was added to excel");

            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Critical error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static bool TryGetPaths(bool isNewSpreadsheet, out string ofxFolderPath, out string excelFilePath)
    {
        ofxFolderPath = "";
        excelFilePath = "";

        string selectedFolder = "";
        string selectedExcel = "";

        var folderThread = new Thread(() =>
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select folder containing OFX files"
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
                selectedFolder = folderDialog.SelectedPath;
        });

        folderThread.SetApartmentState(ApartmentState.STA);
        folderThread.Start();
        folderThread.Join();

        if (string.IsNullOrEmpty(selectedFolder))
            return false;

        ofxFolderPath = selectedFolder;

        var excelThread = new Thread(() =>
        {
            if (isNewSpreadsheet)
            {
                using var saveDialog = new SaveFileDialog
                {
                    Title = "Save new Excel file",
                    Filter = "Excel|*.xlsx",
                    DefaultExt = "xlsx"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                    selectedExcel = saveDialog.FileName;
            }
            else
            {
                using var openDialog = new OpenFileDialog
                {
                    Title = "Select existing Excel",
                    Filter = "Excel|*.xlsx"
                };

                if (openDialog.ShowDialog() == DialogResult.OK)
                    selectedExcel = openDialog.FileName;
            }
        });

        excelThread.SetApartmentState(ApartmentState.STA);
        excelThread.Start();
        excelThread.Join();

        if (string.IsNullOrEmpty(selectedExcel))
            return false;

        excelFilePath = selectedExcel;

        return true;
    }
}