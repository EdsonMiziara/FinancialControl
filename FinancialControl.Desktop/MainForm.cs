using FinancialControl.Shared.Models;
using FinancialControl.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinancialControl.Desktop
{
    public partial class MainForm : Form
    {
        private readonly AppDbContext _context;
        private readonly FileService _fileService;
        private readonly CategorizerService _categorizer;

        public MainForm(AppDbContext context, FileService fileService, CategorizerService categorizer)
        {
            InitializeComponent();

            _context = context;
            _fileService = fileService;
            _categorizer = categorizer;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            SetupUI();
            await CarregarGrid();
            await AtualizarResumo();
            await CarregarCategorias();
        }
        

        // ================= UI =================
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

            gridTransacoes.CellFormatting += gridTransacoes_CellFormatting;

            // HEADER
            labelTituloPagina.ForeColor = Color.FromArgb(40, 40, 40);

            // CARDS
            ConfigurarCard(cardEntrada, Color.FromArgb(40, 167, 69));
            ConfigurarCard(cardSaida, Color.FromArgb(220, 53, 69));
            ConfigurarCard(cardSaldo, Color.FromArgb(0, 123, 255));
        }

        private void ConfigurarCard(Panel card, Color cor)
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


        // ================= DADOS =================
        private async Task CarregarGrid()
        {
            var dados = await _context.Transacoes
                .Include(x => x.Categoria) // 🔥 IMPORTANTE
                .OrderByDescending(x => x.Data)
                .Take(300)
                .Select(x => new
                {
                    x.Id,
                    x.Data,
                    x.Valor,
                    x.Descricao,
                    x.Tipo,
                    Categoria = x.Categoria != null ? x.Categoria.Nome : "Extra",
                    x.NomeOriginal
                })
                .ToListAsync();

            gridTransacoes.DataSource = dados;

            gridTransacoes.Columns["Valor"].DefaultCellStyle.Format = "C2";
            gridTransacoes.Columns["Data"].DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        private async Task AtualizarResumo()
        {
            var entrada = await _context.Transacoes
                .Where(x => x.Valor > 0)
                .SumAsync(x => (decimal?)x.Valor) ?? 0;

            var saida = await _context.Transacoes
                .Where(x => x.Valor < 0)
                .SumAsync(x => (decimal?)x.Valor) ?? 0;

            var saldo = entrada + saida;

            lblValorEntrada.Text = entrada.ToString("C");
            lblValorSaida.Text = saida.ToString("C");
            lblValorSaldo.Text = saldo.ToString("C");

            // 🔥 cores dinâmicas
            lblValorEntrada.ForeColor = Color.Green;
            lblValorSaida.ForeColor = Color.Red;
            lblValorSaldo.ForeColor = saldo >= 0 ? Color.Green : Color.Red;
        }

        // ================= AÇÕES =================
        private async void btnImportar_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "OFX (*.ofx)|*.ofx"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            int adicionados = await _fileService.ProcessSingleOfx(dialog.FileName);

            MessageBox.Show($"{adicionados} transações importadas!");

            await CarregarGrid();
            await AtualizarResumo();
        }

        // ================= ESTILO GRID =================
        private void gridTransacoes_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var row = gridTransacoes.Rows[e.RowIndex];
            var t = row.DataBoundItem as Transacao;

            if (t == null) return;

            if (t.Valor < 0)
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

        private async void gridTransacoes_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var row = gridTransacoes.Rows[e.RowIndex];
            var transacao = (Transacao)row.DataBoundItem;

            await _categorizer.LearnAsync(transacao.Descricao, transacao.CategoriaId);
        }
        private async Task CarregarCategorias()
        {
            var lista = await _context.Categorias
                .AsNoTracking()
                .ToListAsync();

            comboCategorias.DataSource = lista;
            comboCategorias.DisplayMember = "Nome";
            comboCategorias.ValueMember = "Id";
        }
        private ComboBox comboCategorias;

        // ================= COMPONENTES =================
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
