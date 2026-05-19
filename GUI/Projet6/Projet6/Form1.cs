using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;

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
            this.Size = new Size(1000, 600);
            this.Text = "Caisse Enregistreuse (POS) - Système Multi-Langages";
            this.StartPosition = FormStartPosition.CenterScreen;

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            
            // --- ONGLET 1 : CAISSE (POSForm) ---
            TabPage tabPos = new TabPage("🛒 Caisse Principale");
            dgvPosProducts = CreateGrid(20, 20, 450, 400);
            dgvPosCart = CreateGrid(500, 20, 450, 350);
            dgvPosCart.Columns.Add("Id", "ID");
            dgvPosCart.Columns.Add("Nom", "Article");
            dgvPosCart.Columns.Add("Quantite", "Qté");
            dgvPosCart.Columns.Add("PrixUnit", "Prix Unit.");

            Button btnAddCart = new Button { Text = "Ajouter ->", Location = new Point(20, 440), Size = new Size(150, 40), BackColor = Color.LightBlue };
            btnAddCart.Click += BtnAddCart_Click;

            lblTotalTtc = new Label { Text = "Total TTC : 0.00 €", Location = new Point(500, 380), Size = new Size(250, 30), Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.DarkRed };
            
            Button btnPay = new Button { Text = "💳 PROCÉDER AU PAIEMENT", Location = new Point(700, 420), Size = new Size(250, 60), BackColor = Color.LightGreen, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            btnPay.Click += BtnPay_Click;

            tabPos.Controls.AddRange(new Control[] { dgvPosProducts, dgvPosCart, btnAddCart, lblTotalTtc, btnPay });

            // --- ONGLET 2 : CATALOGUE (Products Form) ---
            TabPage tabCat = new TabPage("📦 Gestion Catalogue");
            dgvCatalogue = CreateGrid(20, 20, 600, 450);
            
            GroupBox grpAdd = new GroupBox { Text = "Nouveau Produit", Location = new Point(640, 20), Size = new Size(300, 280) };
            grpAdd.Controls.Add(new Label { Text = "Nom :", Location = new Point(20, 30) });
            txtName = new TextBox { Location = new Point(100, 30), Width = 180 };
            grpAdd.Controls.Add(new Label { Text = "Prix HT :", Location = new Point(20, 80) });
            txtPrice = new TextBox { Location = new Point(100, 80), Width = 180 };
            grpAdd.Controls.Add(new Label { Text = "TVA :", Location = new Point(20, 130) });
            cbVat = new ComboBox { Location = new Point(100, 130), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cbVat.Items.AddRange(new object[] { "0.06", "0.12", "0.21" }); cbVat.SelectedIndex = 2;
            grpAdd.Controls.Add(new Label { Text = "Stock :", Location = new Point(20, 180) });
            txtStock = new TextBox { Location = new Point(100, 180), Width = 180 };
            Button btnAddProduct = new Button { Text = "Ajouter", Location = new Point(100, 230), Size = new Size(180, 35), BackColor = Color.LightSkyBlue };
            btnAddProduct.Click += BtnAddProduct_Click;
            grpAdd.Controls.AddRange(new Control[] { txtName, txtPrice, cbVat, txtStock, btnAddProduct });

            Button btnDelete = new Button { Text = "❌ Supprimer Sélection", Location = new Point(640, 320), Size = new Size(300, 40), BackColor = Color.Salmon };
            btnDelete.Click += BtnDelete_Click;

            tabCat.Controls.AddRange(new Control[] { dgvCatalogue, grpAdd, btnDelete });

            // --- ONGLET 3 : STATISTIQUES (StatsForm) ---
            TabPage tabStats = new TabPage("📊 Statistiques du Jour");
            lblCa = new Label { Text = "Chiffre d'Affaires TTC : --- €", Location = new Point(50, 50), Size = new Size(400, 30), Font = new Font("Segoe UI", 14) };
            lblTickets = new Label { Text = "Nombre de Tickets : ---", Location = new Point(50, 100), Size = new Size(400, 30), Font = new Font("Segoe UI", 14) };
            lblTva = new Label { Text = "TVA Collectée : --- €", Location = new Point(50, 150), Size = new Size(400, 30), Font = new Font("Segoe UI", 14) };
            Button btnRefreshStats = new Button { Text = "Rafraîchir", Location = new Point(50, 220), Size = new Size(150, 40) };
            btnRefreshStats.Click += async (s, e) => await LoadStats();
            tabStats.Controls.AddRange(new Control[] { lblCa, lblTickets, lblTva, btnRefreshStats });

            // Assemblage
            tabControl.TabPages.Add(tabPos);
            tabControl.TabPages.Add(tabCat);
            tabControl.TabPages.Add(tabStats);
            this.Controls.Add(tabControl);

            this.Load += async (s, e) => { await LoadProducts(); await LoadStats(); };
        }

        private DataGridView CreateGrid(int x, int y, int w, int h)
        {
            return new DataGridView { Location = new Point(x, y), Size = new Size(w, h), ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false };
        }

        // --- METHODES ---
        private async System.Threading.Tasks.Task LoadProducts()
        {
            try
            {
                var products = await _api.GetProductsAsync();
                var dt = new System.Data.DataTable();
                dt.Columns.Add("ID"); dt.Columns.Add("Nom"); dt.Columns.Add("Prix HT"); dt.Columns.Add("TVA"); dt.Columns.Add("Stock");

                foreach (var p in products.EnumerateArray())
                {
                    dt.Rows.Add(p.GetProperty("id").GetInt32(), p.GetProperty("name").GetString(), p.GetProperty("price_ht").GetDouble() + " €", (p.GetProperty("vat_rate").GetDouble() * 100) + " %", p.GetProperty("stock").GetInt32());
                }
                dgvPosProducts.DataSource = dt;
                dgvCatalogue.DataSource = dt;
            }
            catch (Exception ex) { MessageBox.Show("Erreur API : " + ex.Message); }
        }

        private async System.Threading.Tasks.Task LoadStats()
        {
            try
            {
                var stats = await _api.GetDailyStatsAsync();
                lblCa.Text = $"Chiffre d'Affaires TTC : {stats.GetProperty("ca_ttc").GetDouble()} €";
                lblTickets.Text = $"Nombre de Tickets : {stats.GetProperty("n_tickets").GetInt32()}";
                lblTva.Text = $"TVA Collectée : {stats.GetProperty("tva_total").GetDouble()} €";
            }
            catch { }
        }

        private void BtnAddCart_Click(object sender, EventArgs e)
        {
            if (dgvPosProducts.SelectedRows.Count == 0) return;
            var row = dgvPosProducts.SelectedRows[0];
            string id = row.Cells["ID"].Value.ToString();
            string nom = row.Cells["Nom"].Value.ToString();
            string prixStr = row.Cells["Prix HT"].Value.ToString().Replace(" €", "");

            bool exists = false;
            foreach (DataGridViewRow r in dgvPosCart.Rows)
            {
                if (r.Cells["Id"].Value?.ToString() == id) {
                    r.Cells["Quantite"].Value = Convert.ToInt32(r.Cells["Quantite"].Value) + 1;
                    exists = true; break;
                }
            }
            if (!exists) dgvPosCart.Rows.Add(id, nom, 1, prixStr);
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            double total = 0;
            foreach (DataGridViewRow r in dgvPosCart.Rows)
            {
                // Approximation du TTC pour l'affichage live (le vrai sera fait par Flask/C)
                double prix = Convert.ToDouble(r.Cells["PrixUnit"].Value) * 1.21; // TVA moyenne estimée pour la UI
                total += prix * Convert.ToInt32(r.Cells["Quantite"].Value);
            }
            lblTotalTtc.Text = $"Total TTC : ~{total:F2} €";
        }

        private async void BtnAddProduct_Click(object sender, EventArgs e)
        {
            try {
                var p = new { name = txtName.Text, price_ht = Convert.ToDouble(txtPrice.Text), vat_rate = Convert.ToDouble(cbVat.Text), stock = Convert.ToInt32(txtStock.Text) };
                await _api.AddProductAsync(p);
                await LoadProducts();
                txtName.Clear(); txtPrice.Clear(); txtStock.Clear();
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvCatalogue.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvCatalogue.SelectedRows[0].Cells["ID"].Value);
            try { await _api.DeleteProductAsync(id); await LoadProducts(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Erreur"); }
        }

        private async void BtnPay_Click(object sender, EventArgs e)
        {
            if (dgvPosCart.Rows.Count == 0) return;

            // Ouvre le PaymentForm (Saisie du montant)
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
                        
                        // Ouvre le ReceiptForm (Le vrai ticket)
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

    // --- FORMULAIRES SECONDAIRES (Exigés par le CDC) ---

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
            
            Button btnValider = new Button { Text = "Valider", Location = new Point(20, 100), Size = new Size(110, 40), BackColor = Color.LightGreen };
            btnValider.Click += (s, e) => { AmountGiven = Convert.ToDouble(txtAmount.Text); this.DialogResult = DialogResult.OK; this.Close(); };
            
            Button btnCancel = new Button { Text = "Annuler", Location = new Point(150, 100), Size = new Size(110, 40), BackColor = Color.LightSalmon };
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