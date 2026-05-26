using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;
using System.Globalization;

namespace Projet6
{
    public partial class Form1 : Form
    {
        private ApiService _api;
        
        // Composants de la Caisse
        private DataGridView dgvPosProducts, dgvPosCart;
        private Label lblTotalTtc;
        
        // Composants du Catalogue
        private DataGridView dgvCatalogue;
        private TextBox txtName, txtPrice, txtStock;
        private ComboBox cbVat;
        
        // Composants des Stats
        private Label lblCa, lblTickets, lblTva;

        public Form1()
        {
            InitializeComponent();
            _api = new ApiService();
            InitializeFullApp();
        }

        private void InitializeFullApp()
        {
            this.Size = new Size(1020, 620);
            this.Text = "Caisse Enregistreuse (POS) - Système Multi-Langages";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            
            // --- ONGLET 1 : CAISSE (POSForm) ---
            TabPage tabPos = new TabPage("🛒 Caisse Principale");
            tabPos.BackColor = Color.White;
            
            dgvPosProducts = CreateGrid(20, 20, 450, 400);
            dgvPosCart = CreateGrid(500, 20, 460, 350);
            dgvPosCart.Columns.Add("Id", "ID");
            dgvPosCart.Columns.Add("Nom", "Article");
            dgvPosCart.Columns.Add("Quantite", "Qté");
            dgvPosCart.Columns.Add("PrixUnit", "Prix Unit. HT");
            dgvPosCart.Columns.Add("TVA", "TVA");

            Button btnAddCart = new Button { Text = "Ajouter au panier ➡️", Location = new Point(20, 435), Size = new Size(180, 40), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnAddCart.FlatAppearance.BorderSize = 0;
            btnAddCart.Click += BtnAddCart_Click;

            lblTotalTtc = new Label { Text = "Total TTC : 0.00 €", Location = new Point(500, 385), Size = new Size(300, 35), Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(192, 57, 43), TextAlign = ContentAlignment.MiddleLeft };
            
            Button btnPay = new Button { Text = "💳 PROCÉDER AU PAIEMENT", Location = new Point(710, 435), Size = new Size(250, 55), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnPay.FlatAppearance.BorderSize = 0;
            btnPay.Click += BtnPay_Click;

            tabPos.Controls.AddRange(new Control[] { dgvPosProducts, dgvPosCart, btnAddCart, lblTotalTtc, btnPay });

            // --- ONGLET 2 : CATALOGUE (Products Form) ---
            TabPage tabCat = new TabPage("📦 Gestion Catalogue");
            tabCat.BackColor = Color.White;
            
            dgvCatalogue = CreateGrid(20, 20, 600, 460);
            dgvCatalogue.SelectionChanged += DgvCatalogue_SelectionChanged; // Remplissage auto lors du clic
            
            GroupBox grpAdd = new GroupBox { Text = "Fiche Produit (Saisie / Édition)", Location = new Point(640, 12), Size = new Size(330, 290), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            
            Label lblName = new Label { Text = "Nom :", Location = new Point(15, 35), Size = new Size(75, 25), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.5f) };
            txtName = new TextBox { Location = new Point(95, 35), Width = 210, Font = new Font("Segoe UI", 10) };
            
            Label lblPrice = new Label { Text = "Prix HT :", Location = new Point(15, 85), Size = new Size(75, 25), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.5f) };
            txtPrice = new TextBox { Location = new Point(95, 85), Width = 210, Font = new Font("Segoe UI", 10) };
            
            Label lblVat = new Label { Text = "TVA :", Location = new Point(15, 135), Size = new Size(75, 25), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.5f) };
            cbVat = new ComboBox { Location = new Point(95, 135), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            cbVat.Items.AddRange(new object[] { "0.06", "0.12", "0.21" }); cbVat.SelectedIndex = 2;
            
            Label lblStock = new Label { Text = "Stock :", Location = new Point(15, 185), Size = new Size(75, 25), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.5f) };
            txtStock = new TextBox { Location = new Point(95, 185), Width = 210, Font = new Font("Segoe UI", 10) };
            
            Button btnAddProduct = new Button { Text = "➕ Ajouter comme Nouveau", Location = new Point(95, 235), Size = new Size(210, 38), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnAddProduct.FlatAppearance.BorderSize = 0;
            btnAddProduct.Click += BtnAddProduct_Click;
            
            grpAdd.Controls.AddRange(new Control[] { lblName, txtName, lblPrice, txtPrice, lblVat, cbVat, lblStock, txtStock, btnAddProduct });

            Button btnEdit = new Button { Text = "✏️ Enregistrer les Modifications", Location = new Point(640, 320), Size = new Size(330, 42), BackColor = Color.FromArgb(241, 196, 15), ForeColor = Color.Black, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += BtnEdit_Click;

            Button btnDelete = new Button { Text = "❌ Supprimer l'Article Sélectionné", Location = new Point(640, 375), Size = new Size(330, 42), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            tabCat.Controls.AddRange(new Control[] { dgvCatalogue, grpAdd, btnDelete, btnEdit });
            
            // --- ONGLET 3 : STATISTIQUES ---
            TabPage tabStats = new TabPage("📊 Statistiques du Jour");
            tabStats.BackColor = Color.White;
            lblCa = new Label { Text = "Chiffre d'Affaires TTC : --- €", Location = new Point(50, 50), Size = new Size(500, 35), Font = new Font("Segoe UI", 14) };
            lblTickets = new Label { Text = "Nombre de Tickets : ---", Location = new Point(50, 100), Size = new Size(500, 35), Font = new Font("Segoe UI", 14) };
            lblTva = new Label { Text = "TVA Collectée : --- €", Location = new Point(50, 150), Size = new Size(500, 35), Font = new Font("Segoe UI", 14) };
            
            Button btnRefreshStats = new Button { Text = "🔄 Rafraîchir les Données", Location = new Point(50, 220), Size = new Size(220, 42), BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnRefreshStats.FlatAppearance.BorderSize = 0;
            btnRefreshStats.Click += async (s, e) => await LoadStats();
            tabStats.Controls.AddRange(new Control[] { lblCa, lblTickets, lblTva, btnRefreshStats });

            tabControl.TabPages.Add(tabPos);
            tabControl.TabPages.Add(tabCat);
            tabControl.TabPages.Add(tabStats);
            this.Controls.Add(tabControl);

            this.Load += async (s, e) => { await LoadProducts(); await LoadStats(); };
        }

        // Système de génération de tableaux modernes et propres
        private DataGridView CreateGrid(int x, int y, int w, int h)
        {
            var dgv = new DataGridView
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible = false,
                GridColor = Color.FromArgb(236, 240, 241),
                EnableHeadersVisualStyles = false
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersHeight = 35;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 244, 248);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgv.RowTemplate.Height = 32;

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            return dgv;
        }

        private void DgvCatalogue_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCatalogue.SelectedRows.Count > 0)
            {
                var row = dgvCatalogue.SelectedRows[0];
                txtName.Text = row.Cells["Nom"].Value?.ToString();
                txtPrice.Text = row.Cells["Prix HT"].Value?.ToString().Replace(" €", "").Trim();
                
                string tvaStr = row.Cells["TVA"].Value?.ToString().Replace(" %", "").Trim();
                if (tvaStr == "6") cbVat.SelectedItem = "0.06";
                else if (tvaStr == "12") cbVat.SelectedItem = "0.12";
                else if (tvaStr == "21") cbVat.SelectedItem = "0.21";

                txtStock.Text = row.Cells["Stock"].Value?.ToString();
            }
        }

        private async System.Threading.Tasks.Task LoadProducts()
        {
            try
            {
                var products = await _api.GetProductsAsync();
                var dt = new System.Data.DataTable();
                dt.Columns.Add("ID"); dt.Columns.Add("Nom"); dt.Columns.Add("Prix HT"); dt.Columns.Add("TVA"); dt.Columns.Add("Stock");

                foreach (var p in products.EnumerateArray())
                {
                    dt.Rows.Add(
                        p.GetProperty("id").GetInt32(), 
                        p.GetProperty("name").GetString(), 
                        p.GetProperty("price_ht").GetDouble().ToString(CultureInfo.InvariantCulture) + " €", 
                        (p.GetProperty("vat_rate").GetDouble() * 100).ToString(CultureInfo.InvariantCulture) + " %", 
                        p.GetProperty("stock").GetInt32()
                    );
                }
                dgvPosProducts.DataSource = dt;
                dgvCatalogue.DataSource = dt;

                foreach (DataGridViewRow row in dgvCatalogue.Rows)
                {
                    if (row.Cells["Stock"].Value != null && Convert.ToInt32(row.Cells["Stock"].Value) < 5)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 237, 238);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(192, 57, 43);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur API : " + ex.Message); }
        }

        private async System.Threading.Tasks.Task LoadStats()
        {
            try
            {
                var stats = await _api.GetDailyStatsAsync();
                lblCa.Text = $"Chiffre d'Affaires TTC : {stats.GetProperty("ca_ttc").GetDouble().ToString("F2")} €";
                lblTickets.Text = $"Nombre de Tickets : {stats.GetProperty("n_tickets").GetInt32()}";
                lblTva.Text = $"TVA Collectée : {stats.GetProperty("tva_total").GetDouble().ToString("F2")} €";
            }
            catch { }
        }

        private void BtnAddCart_Click(object sender, EventArgs e)
        {
            if (dgvPosProducts.SelectedRows.Count == 0) return;
            var row = dgvPosProducts.SelectedRows[0];
            string id = row.Cells["ID"].Value.ToString();
            string nom = row.Cells["Nom"].Value.ToString();
            string prixStr = row.Cells["Prix HT"].Value.ToString().Replace(" €", "").Trim();
            string tvaStr = row.Cells["TVA"].Value.ToString().Replace(" %", "").Trim();

            bool exists = false;
            foreach (DataGridViewRow r in dgvPosCart.Rows)
            {
                if (r.Cells["Id"].Value?.ToString() == id) {
                    r.Cells["Quantite"].Value = Convert.ToInt32(r.Cells["Quantite"].Value) + 1;
                    exists = true; break;
                }
            }
            if (!exists) dgvPosCart.Rows.Add(id, nom, 1, prixStr, tvaStr);
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            double total = 0;
            foreach (DataGridViewRow r in dgvPosCart.Rows)
            {
                double prixHt = Convert.ToDouble(r.Cells["PrixUnit"].Value.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                double tvaPercent = Convert.ToDouble(r.Cells["TVA"].Value.ToString().Replace(",", "."), CultureInfo.InvariantCulture) / 100.0;
                double prixTtc = prixHt * (1 + tvaPercent);
                total += prixTtc * Convert.ToInt32(r.Cells["Quantite"].Value);
            }
            lblTotalTtc.Text = $"Total TTC : ~{total:F2} €";
        }

        private async void BtnAddProduct_Click(object sender, EventArgs e)
        {
            try {
                var p = new { 
                    name = txtName.Text, 
                    price_ht = Convert.ToDouble(txtPrice.Text.Replace(",", "."), CultureInfo.InvariantCulture), 
                    vat_rate = Convert.ToDouble(cbVat.Text.Replace(",", "."), CultureInfo.InvariantCulture), 
                    stock = Convert.ToInt32(txtStock.Text) 
                };
                await _api.AddProductAsync(p);
                await LoadProducts();
                txtName.Clear(); txtPrice.Clear(); txtStock.Clear();
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvCatalogue.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvCatalogue.SelectedRows[0].Cells["ID"].Value);
            try { 
                await _api.DeleteProductAsync(id); 
                await LoadProducts(); 
                txtName.Clear(); txtPrice.Clear(); txtStock.Clear();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Erreur"); }
        }

        private async void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvCatalogue.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvCatalogue.SelectedRows[0].Cells["ID"].Value);

            try {
                var p = new { 
                    name = txtName.Text, 
                    price_ht = Convert.ToDouble(txtPrice.Text.Replace(",", "."), CultureInfo.InvariantCulture), 
                    vat_rate = Convert.ToDouble(cbVat.Text.Replace(",", "."), CultureInfo.InvariantCulture), 
                    stock = Convert.ToInt32(txtStock.Text) 
                };
                await _api.UpdateProductAsync(id, p);
                await LoadProducts();
                MessageBox.Show("Produit mis à jour avec succès !", "Succès");
                txtName.Clear(); txtPrice.Clear(); txtStock.Clear();
            } 
            catch (Exception) { 
                MessageBox.Show("Veuillez remplir les champs de saisie correctement.", "Erreur de Saisie"); 
            }
        }

        private async void BtnPay_Click(object sender, EventArgs e)
        {
            if (dgvPosCart.Rows.Count == 0) return;

            using (var paymentForm = new PaymentForm())
            {
                if (paymentForm.ShowDialog() == DialogResult.OK)
                {
                    var itemsList = new List<object>();
                    foreach (DataGridViewRow r in dgvPosCart.Rows)
                        if (r.Cells["Id"].Value != null)
                            itemsList.Add(new { product_id = Convert.ToInt32(r.Cells["Id"].Value), qty = Convert.ToInt32(r.Cells["Quantite"].Value) });

                    var transaction = new { items = itemsList, amount_given = paymentForm.AmountGiven };

                    try
                    {
                        var result = await _api.PostTransactionAsync(transaction);
                        
                        using (var receiptForm = new ReceiptForm(result, paymentForm.AmountGiven)) {
                            receiptForm.ShowDialog();
                        }

                        dgvPosCart.Rows.Clear(); lblTotalTtc.Text = "Total TTC : 0.00 €";
                        await LoadProducts(); await LoadStats();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Paiement Refusé", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                }
            }
        }
    }

    public class PaymentForm : Form
    {
        public double AmountGiven { get; private set; }
        private TextBox txtAmount;

        public PaymentForm()
        {
            this.Text = "Paiement Client"; this.Size = new Size(300, 200); this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;

            Controls.Add(new Label { Text = "Montant donné par le client (€) :", Location = new Point(20, 20), Width = 250 });
            txtAmount = new TextBox { Location = new Point(20, 50), Width = 240, Font = new Font("Segoe UI", 12) };
            
            Button btnValider = new Button { Text = "Valider", Location = new Point(20, 100), Size = new Size(110, 40), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnValider.FlatAppearance.BorderSize = 0;
            btnValider.Click += (s, e) => { 
                AmountGiven = Convert.ToDouble(txtAmount.Text.Replace(",", "."), CultureInfo.InvariantCulture); 
                this.DialogResult = DialogResult.OK; this.Close(); 
            };
            
            Button btnCancel = new Button { Text = "Annuler", Location = new Point(150, 100), Size = new Size(110, 40), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            Controls.AddRange(new Control[] { txtAmount, btnValider, btnCancel });
        }
    }

    public class ReceiptForm : Form
    {
        public ReceiptForm(JsonElement apiResult, double amountGiven)
        {
            this.Text = "Ticket de Caisse"; this.Size = new Size(350, 450); this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            double totalTtc = apiResult.GetProperty("total_ttc").GetDouble();
            
            string ticketText = "=== MON PETIT COMMERCE ===\n\n";
            ticketText += $"Date : {DateTime.Now}\n";
            ticketText += $"Transaction N° : {apiResult.GetProperty("transaction_id").GetInt32()}\n\n";
            ticketText += $"TOTAL TTC : {totalTtc:F2} €\n";
            ticketText += $"Payé : {amountGiven:F2} €\n";
            ticketText += $"A Rendre : {(amountGiven - totalTtc):F2} €\n\n";
            ticketText += "DÉTAIL DU RENDU (Calcul C) :\n";

            var change = apiResult.GetProperty("change_returned");
            foreach (var coin in change.EnumerateObject())
            {
                if (coin.Value.GetInt32() > 0)
                    ticketText += $" -> {coin.Value.GetInt32()} x {coin.Name} €\n";
            }
            ticketText += "\nMerci de votre visite !";

            Controls.Add(new Label { Text = ticketText, Location = new Point(20, 20), Size = new Size(300, 380), Font = new Font("Courier New", 10) });
        }
    }
}