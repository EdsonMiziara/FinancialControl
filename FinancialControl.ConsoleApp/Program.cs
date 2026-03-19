using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.Shared;
using FinancialControl.Shared.Interfaces;
using FinancialControl.Shared.Interfaces;
using FinancialControl.Shared.Services;
using FinancialControl.Shared.Services;
using FinancialControl.Shared.SupportModels;
using FinancialControl.Shared.SupportModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using System.Windows.Forms;

public static class Program
{
    [STAThread]
    static async Task Main()
    {
        Application.EnableVisualStyles();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection");

        // ✅ CORRETO
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;

        var dbContext = new AppDbContext(options);

        ITransacaoRepository repository = new TransacaoRepository(configuration);
        var loader = new CategorizerLoader(dbContext);
        var cache = await loader.LoadAsync();

        var categorizer = new CategorizerService(cache, dbContext);

        FileService fileService = new FileService(repository, categorizer);


        var choice = MessageBox.Show(
            "Deseja utilizar uma planilha de controle existente?\n\n(Sim = Selecionar Existente | Não = Criar Nova)",
            "Configuração",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (choice == DialogResult.Cancel) return;
        bool isNew = (choice == DialogResult.No);

        if (!TryObtainPath(isNew, out string ofxFolder, out string excelPath)) return;

        try
        {
            var mapping = new ColumnMap();
            using var excelService = new SpreadSheetService(excelPath, isNew, mapping);
            var ws = excelService.ObtainSpreadsheet();

            var columns = isNew ? mapping : FileService.ColumnMapping(ws);
            var existent = isNew ? new HashSet<string>() : FileService.LoadExistentTransactions(ws, columns);

            int currentLine = ws.LastRowUsed()?.RowNumber() + 1 ?? columns.HeaderLine + 1;

            int totalAdded = await fileService.ProcessOfxFile(ofxFolder, ws, columns, existent, currentLine);

            if (totalAdded > 0)
            {
                excelService.Save(currentLine - 1);
                MessageBox.Show($"Sucesso! {totalAdded} novas transações adicionadas.", "Concluído");
            }
            else
            {
                MessageBox.Show("Nenhuma transação nova encontrada.", "Aviso");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro crítico: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


public static bool TryObtainPath(bool isNew, out string folder, out string excel)
    {
        folder = excel = "";

        using (var fbd = new FolderBrowserDialog { Description = "Selecione a pasta contendo os arquivos OFX" })
        {
            if (fbd.ShowDialog() != DialogResult.OK) return false;
            folder = fbd.SelectedPath;
        }

        if (isNew)
        {
            using var sfd = new SaveFileDialog { Title = "Salvar novo Controle Financeiro", Filter = "Excel|*.xlsx", DefaultExt = "xlsx" };
            if (sfd.ShowDialog() != DialogResult.OK) return false;
            excel = sfd.FileName;
        }
        else
        {
            using var ofd = new OpenFileDialog { Title = "Selecione o Excel existente", Filter = "Excel|*.xlsx" };
            if (ofd.ShowDialog() != DialogResult.OK) return false;
            excel = ofd.FileName;
        }

        return true;
    }
}