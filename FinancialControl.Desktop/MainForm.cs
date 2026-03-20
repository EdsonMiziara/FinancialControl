using FinancialControl.Shared.Models;
using FinancialControl.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FinancialControl.Desktop
{
    public partial class MainForm : Form
    {
        private readonly AppDbContext _context;
        private readonly FileService _fileService;
        private readonly CategorizerService _categorizer;

        /// <summary>
        /// Constructor for the MainForm class, which initializes the form and its dependencies,
        /// including the database context, file service, and categorizer service.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fileService"></param>
        /// <param name="categorizer"></param>
        
        public MainForm(AppDbContext context, FileService fileService, CategorizerService categorizer)
        {
            InitializeComponent();

            _context = context;
            _fileService = fileService;
            _categorizer = categorizer;

            this.Load += Form1_Load;
        }

        /// <summary>
        /// Load event handler for the MainForm. This method is called when the form is first loaded
        /// and is responsible for setting up the user interface,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        
        private async void Form1_Load(object sender, EventArgs e)
        {
            SetupUI();
            await LoadGrid();
            await UpdateSummary();
            await LoadCategories();
        }

        /// <summary>
        /// Setups the user interface for the main form, including styling for the DataGridView, header, and summary cards.
        /// </summary>

        private void SetupUI()
        {
            // GRID
            gridTransacoes.BorderStyle = BorderStyle.None;
            gridTransacoes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            gridTransacoes.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            gridTransacoes.RowHeadersVisible = false;
            gridTransacoes.EnableHeadersVisualStyles = false;

            gridTransacoes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            gridTransacoes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridTransacoes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            gridTransacoes.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            gridTransacoes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            gridTransacoes.CellFormatting += gridTransactions_CellFormatting;

            // HEADER
            labelTituloPagina.ForeColor = Color.FromArgb(40, 40, 40);

            // CARDS
            ConfigureCard(cardEntrada, Color.FromArgb(40, 167, 69));
            ConfigureCard(cardSaida, Color.FromArgb(220, 53, 69));
            ConfigureCard(cardSaldo, Color.FromArgb(0, 123, 255));
        }

        /// <summary>
        /// Configures a card panel with a colored left border and consistent styling.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="cor"></param>
        private void ConfigureCard(Panel card, Color cor)
        {
            card.BackColor = Color.White;
            card.Padding = new Padding(15);

            Panel barra = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = cor
            };

            card.Controls.Add(barra);
            barra.BringToFront();
        }


        /// <summary>
        /// Loads the latest transactions from the database, including their categories, and binds them to the DataGridView.
        /// </summary>
        /// <returns>
        /// Returns a Task representing the asynchronous operation. The method fetches transactions,
        /// orders them by date, and formats the grid columns for currency and date display.
        /// </returns>
        private async Task LoadGrid()
        {
            var dados = await _context.Transacoes
                .Include(x => x.Category) 
                .OrderByDescending(x => x.Date)
                .Take(300)
                .ToListAsync();

            gridTransacoes.DataSource = dados;

            gridTransacoes.Columns["Valor"].DefaultCellStyle.Format = "C2";
            gridTransacoes.Columns["Data"].DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        /// <summary>
        /// Updates the summary cards for total income, total expenses, and net balance by calculating sums from the transactions in the database.
        /// </summary>
        /// <returns>
        /// Returns a Task representing the asynchronous operation. The method calculates the total income, total expenses, and net balance,
        /// then updates the corresponding labels with formatted currency values and appropriate colors based on the values.
        /// </returns>
        private async Task UpdateSummary()
        {
            var entrada = await _context.Transacoes
                .Where(x => x.Tipe == "INCOME")
                .SumAsync(x => (decimal?)x.Value) ?? 0;

            var saida = await _context.Transacoes
                .Where(x => x.Tipe == "EXPENSE")
                .SumAsync(x => (decimal?)x.Value) ?? 0;

            var saldo = entrada + saida; // saída já é negativa

            lblValorEntrada.Text = entrada.ToString("C");
            lblValorSaida.Text = saida.ToString("C");
            lblValorSaldo.Text = saldo.ToString("C");

            lblValorEntrada.ForeColor = Color.Green;
            lblValorSaida.ForeColor = Color.Red;
            lblValorSaldo.ForeColor = saldo >= 0 ? Color.Green : Color.Red;
        }

        /// <summary>
        /// Vent handler for the "Import" button click event. Opens a file dialog to select an OFX file, processes the file to import transactions,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnImport_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "OFX (*.ofx)|*.ofx"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            int adicionados = await _fileService.ProcessSingleOfx(dialog.FileName);

            MessageBox.Show($"{adicionados} transações importadas!");

            await LoadGrid();
            await UpdateSummary();
        }

        /// <summary>
        /// Cell formatting event handler for the transactions DataGridView. Applies conditional styling to rows based on the transaction value,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridTransactions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var row = gridTransacoes.Rows[e.RowIndex];
            var t = row.DataBoundItem as Transaction;

            if (t == null) return;

            if (t.Value < 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
                row.DefaultCellStyle.ForeColor = Color.DarkRed;
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(235, 255, 235);
                row.DefaultCellStyle.ForeColor = Color.DarkGreen;
            }
        }

        /// <summary>
        /// Cell end edit event handler for the transactions DataGridView.
        /// When a cell edit is completed, it retrieves the edited transaction and uses the categorizer service
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void gridTransactions_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var row = gridTransacoes.Rows[e.RowIndex];
            var transacao = (Transaction)row.DataBoundItem;

            await _categorizer.LearnAsync(transacao.Description, transacao.CategoryId);
        }

        /// <summary>
        /// Loads the list of categories from the database and binds it to the categories ComboBox for selection when editing transactions.
        /// </summary>
        /// <returns></returns>
        private async Task LoadCategories()
        {
            var lista = await _context.Categorias
                .AsNoTracking()
                .ToListAsync();

            comboCategorias.DataSource = lista;
            comboCategorias.DisplayMember = "Nome";
            comboCategorias.ValueMember = "Id";
        }
        private ComboBox comboCategorias;

        // Components
        private Panel panelSidebar;
        private Panel panelMain;
        private Panel panelTop;
        private Panel panelCards;

        private TableLayoutPanel tableCards;

        private Panel cardEntrada;
        private Panel cardSaida;
        private Panel cardSaldo;

        private Label lblLogo;
        private Label labelTituloPagina;

        private Label lblValorEntrada;
        private Label lblValorSaida;
        private Label lblValorSaldo;

        private Label lblTituloEntrada;
        private Label lblTituloSaida;
        private Label lblTituloSaldo;

        private Button btnDashboard;
        private Button btnImportar;
        private Button btnCategorias;

        private DataGridView gridTransacoes;
    }

}
