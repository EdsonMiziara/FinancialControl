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
            gridTransactions.BorderStyle = BorderStyle.None;
            gridTransactions.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            gridTransactions.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            gridTransactions.RowHeadersVisible = false;
            gridTransactions.EnableHeadersVisualStyles = false;

            gridTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            gridTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            gridTransactions.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            gridTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            gridTransactions.CellFormatting += gridTransactions_CellFormatting;

            // HEADER
            labelPageTitle.ForeColor = Color.FromArgb(40, 40, 40);

            // CARDS
            ConfigureCard(cardIncome, Color.FromArgb(40, 167, 69));
            ConfigureCard(cardExpense, Color.FromArgb(220, 53, 69));
            ConfigureCard(cardBalance, Color.FromArgb(0, 123, 255));
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
            var dados = await _context.Transactions
                .Include(x => x.Category)
                .OrderByDescending(x => x.Date)
                .Take(300)
                .Select(x => new
                {
                    x.Id,
                    x.Date,
                    x.Value,
                    x.Description,
                    x.Type,
                    x.CategoryId,
                    Category = x.Category != null ? x.Category.Name : ""
                })
                .ToListAsync();

            gridTransactions.DataSource = dados;

            gridTransactions.Columns["Value"].DefaultCellStyle.Format = "C2";
            gridTransactions.Columns["Date"].DefaultCellStyle.Format = "dd/MM/yyyy";
            gridTransactions.Columns["Category"].HeaderText = "Category";
            gridTransactions.Columns["Value"].HeaderText = "Value";
            gridTransactions.Columns["Date"].HeaderText = "Date";
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
            var income = await _context.Transactions
                .Where(x => x.Value > 0)
                .SumAsync(x => (decimal?)x.Value) ?? 0;

            var expense = await _context.Transactions
                .Where(x => x.Value < 0)
                .SumAsync(x => (decimal?)x.Value) ?? 0;

            var balance = income + expense; // saída já é negativa

            lblIncomeValue.Text = income.ToString("C");
            lblExpenseValue.Text = expense.ToString("C");
            lblBalanceValue.Text = balance.ToString("C");

            lblIncomeValue.ForeColor = Color.Green;
            lblExpenseValue.ForeColor = Color.Red;
            lblBalanceValue.ForeColor = balance >= 0 ? Color.Green : Color.Red;
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
                Filter = "OFX (*.ofx)|*.ofx",
                Multiselect = true,
                Title = "Select one or more OFX files"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            int totalImported = 0;

            if (dialog.FileNames.Length == 1)
            {
                totalImported = await _fileService.ProcessSingleOfxToDb(dialog.FileName);
            }
            else
            {
                foreach (var filePath in dialog.FileNames)
                {
                    totalImported += await _fileService.ProcessSingleOfxToDb(filePath);
                }
            }

            MessageBox.Show($"{totalImported} transactions imported!");

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
            var row = gridTransactions.Rows[e.RowIndex];
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
            var row = gridTransactions.Rows[e.RowIndex];
            var transacao = (Transaction)row.DataBoundItem;

            await _categorizer.LearnAsync(transacao.Description, transacao.CategoryId);
        }

        /// <summary>
        /// Loads the list of categories from the database and binds it to the categories ComboBox for selection when editing transactions.
        /// </summary>
        /// <returns></returns>
        private async Task LoadCategories()
        {
            var lista = await _context.Categories
                .AsNoTracking()
                .ToListAsync();

            comboCategorias.DataSource = lista;
            comboCategorias.DisplayMember = "Name";
            comboCategorias.ValueMember = "Id";
        }
        private ComboBox comboCategorias;

        // Components
        private Panel panelSidebar;
        private Panel panelMain;
        private Panel panelTop;
        private Panel panelCards;

        private TableLayoutPanel tableCards;

        private Panel cardIncome;
        private Panel cardExpense;
        private Panel cardBalance;

        private Label lblLogo;
        private Label labelPageTitle;

        private Label lblIncomeValue;
        private Label lblExpenseValue;
        private Label lblBalanceValue;

        private Label lblIncomeTitle;
        private Label lblExpenseTitle;
        private Label lblBalanceTitle;

        private Button btnDashboard;
        private Button btnImport;
        private Button btnCategories;

        private DataGridView gridTransactions;
    }

}
