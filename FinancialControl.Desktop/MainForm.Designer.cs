namespace FinancialControl.Desktop
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // ===== SIDEBAR =====
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.FromArgb(30, 30, 47)
            };

            lblLogo = new Label
            {
                Text = "Financial Control",
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            btnDashboard = CriarBotao("📊 Dashboard");
            btnImportar = CriarBotao("📂 Importar OFX");
            btnCategorias = CriarBotao("🏷️ Categorias");

            btnImportar.Click += btnImport_Click;

            panelSidebar.Controls.Add(btnCategorias);
            panelSidebar.Controls.Add(btnImportar);
            panelSidebar.Controls.Add(btnDashboard);
            panelSidebar.Controls.Add(lblLogo);

            // ===== MAIN =====
            panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 246, 250)
            };

            // TOP
            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };

            labelTituloPagina = new Label
            {
                Text = "Dashboard",
                Location = new Point(10, 15),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };

            panelTop.Controls.Add(labelTituloPagina);

            // CARDS
            panelCards = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                Padding = new Padding(10)
            };

            tableCards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3
            };

            tableCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            tableCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            tableCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

            cardEntrada = CriarCard("Entrada", out lblValorEntrada, out lblTituloEntrada);
            cardSaida = CriarCard("Saída", out lblValorSaida, out lblTituloSaida);
            cardSaldo = CriarCard("Saldo", out lblValorSaldo, out lblTituloSaldo);

            tableCards.Controls.Add(cardEntrada, 0, 0);
            tableCards.Controls.Add(cardSaida, 1, 0);
            tableCards.Controls.Add(cardSaldo, 2, 0);

            panelCards.Controls.Add(tableCards);

            // GRID
            gridTransacoes = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // ADD
            panelMain.Controls.Add(gridTransacoes);
            panelMain.Controls.Add(panelCards);
            panelMain.Controls.Add(panelTop);

            Controls.Add(panelMain);
            Controls.Add(panelSidebar);

            Text = "Financial Control";
            WindowState = FormWindowState.Maximized;
            // CATEGORIAS (EXEMPLO)
            comboCategorias = new ComboBox
            {
                Location = new Point(10, 70), // ajusta posição depois
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            panelTop.Controls.Add(comboCategorias);
        }

        private Button CriarBotao(string texto)
        {
            return new Button
            {
                Text = texto,
                Dock = DockStyle.Top,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
        }

        private Panel CriarCard(string titulo, out Label lblValor, out Label lblTitulo)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                BackColor = Color.White
            };

            lblTitulo = new Label
            {
                Text = titulo,
                Location = new Point(10, 10),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9)
            };

            lblValor = new Label
            {
                Text = "R$ 0,00",
                Location = new Point(10, 35),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };

            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblValor);

            return card;
        }
    }

    #endregion
}
