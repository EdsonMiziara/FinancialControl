using FinancialControl.ConsoleApp.SupportModels;
using FinancialControl.Shared.Services;

public static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();

        // 1. Pergunta se o usuário quer um arquivo novo ou existente
        var choice = MessageBox.Show(
            "Deseja utilizar uma planilha de controle existente?\n\n(Sim = Selecionar Existente | Não = Criar Nova)",
            "Configuração",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (choice == DialogResult.Cancel) return;
        bool isNew = (choice == DialogResult.No);

        // 2. Obtém os caminhos via interface visual
        if (!TryObtainPath(isNew, out string ofxFolder, out string excelPath)) return;

        try
        {
            var mapping = new ColumnMap(); // Configuração base
            using var excelService = new SpreadSheetService(excelPath, isNew, mapping);
            var ws = excelService.ObtainSpreadsheet();

            // 3. Se não for novo, remapeia as colunas para garantir que o código encontre os dados
            var columns = isNew ? mapping : FileService.ColumnMapping(ws);
            var existent = isNew ? new HashSet<string>() : FileService.LoadExistentTransactions(ws, columns);

            int currentLine = ws.LastRowUsed()?.RowNumber() + 1 ?? columns.HeaderLine + 1;

            // 4. Processamento dos arquivos
            int totalAdded = FileService.ProcessOfxFile(ofxFolder, ws, columns, existent, ref currentLine);

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