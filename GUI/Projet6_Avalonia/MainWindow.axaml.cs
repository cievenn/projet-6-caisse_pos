using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;

namespace Projet6_Avalonia
{
    public partial class MainWindow : Window
    {
        private ApiService _api;
        
        public ObservableCollection<Product> ProductsList { get; set; } = new();
        public ObservableCollection<CartItem> CartList { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            _api = new ApiService();
            
            DgvPosProducts.ItemsSource = ProductsList;
            DgvCatalogue.ItemsSource = ProductsList;
            DgvPosCart.ItemsSource = CartList;

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            await LoadProducts();
            await LoadStats();
        }

        private async Task LoadProducts()
        {
            try
            {
                var products = await _api.GetProductsAsync();
                ProductsList.Clear();

                foreach (var p in products.EnumerateArray())
                {
                    ProductsList.Add(new Product
                    {
                        Id = p.GetProperty("id").GetInt32(),
                        Name = p.GetProperty("name").GetString(),
                        PriceHT = p.GetProperty("price_ht").GetDouble(),
                        VatRate = p.GetProperty("vat_rate").GetDouble(),
                        Stock = p.GetProperty("stock").GetInt32()
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine("Erreur API : " + ex.Message); }
        }

        private async Task LoadStats()
        {
            try
            {
                var stats = await _api.GetDailyStatsAsync();
                LblCa.Text = $"Chiffre d'Affaires TTC : {stats.GetProperty("ca_ttc").GetDouble().ToString("F2")} €";
                LblTickets.Text = $"Nombre de Tickets : {stats.GetProperty("n_tickets").GetInt32()}";
                LblTva.Text = $"TVA Collectée : {stats.GetProperty("tva_total").GetDouble().ToString("F2")} €";
            }
            catch { }
        }

        private void BtnRefreshStats_Click(object sender, RoutedEventArgs e) => _ = LoadStats();

        private void BtnAddCart_Click(object sender, RoutedEventArgs e)
        {
            if (DgvPosProducts.SelectedItem is not Product selected) return;

            bool exists = false;
            foreach (var item in CartList)
            {
                if (item.Id == selected.Id) {
                    item.Quantite++;
                    exists = true; break;
                }
            }
            if (!exists)
            {
                CartList.Add(new CartItem {
                    Id = selected.Id, Nom = selected.Name, Quantite = 1,
                    PrixUnit = selected.PriceHT, TVA = selected.VatRate
                });
            }
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            double total = 0;
            foreach (var item in CartList) {
                total += (item.PrixUnit * (1 + item.TVA)) * item.Quantite;
            }
            LblTotalTtc.Text = $"Total TTC : {total:F2} €";
        }

        private void DgvCatalogue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgvCatalogue.SelectedItem is Product selected)
            {
                TxtName.Text = selected.Name;
                TxtPrice.Text = selected.PriceHT.ToString(CultureInfo.InvariantCulture);
                TxtStock.Text = selected.Stock.ToString();
                
                string tvaStr = selected.VatRate.ToString(CultureInfo.InvariantCulture);
                foreach (ComboBoxItem item in CbVat.Items) {
                    if (item.Content.ToString() == tvaStr) {
                        CbVat.SelectedItem = item; break;
                    }
                }
            }
        }

        private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            try {
                var selectedVat = (CbVat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "0.21";
                var p = new { 
                    name = TxtName.Text, 
                    price_ht = Convert.ToDouble(TxtPrice.Text.Replace(",", "."), CultureInfo.InvariantCulture), 
                    vat_rate = Convert.ToDouble(selectedVat, CultureInfo.InvariantCulture), 
                    stock = Convert.ToInt32(TxtStock.Text) 
                };
                await _api.AddProductAsync(p);
                await LoadProducts();
                TxtName.Text = ""; TxtPrice.Text = ""; TxtStock.Text = "";
            } catch { }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DgvCatalogue.SelectedItem is not Product selected) return;
            try {
                var selectedVat = (CbVat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "0.21";
                var p = new { 
                    name = TxtName.Text, 
                    price_ht = Convert.ToDouble(TxtPrice.Text.Replace(",", "."), CultureInfo.InvariantCulture), 
                    vat_rate = Convert.ToDouble(selectedVat, CultureInfo.InvariantCulture), 
                    stock = Convert.ToInt32(TxtStock.Text) 
                };
                await _api.UpdateProductAsync(selected.Id, p);
                await LoadProducts();
            } catch { }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DgvCatalogue.SelectedItem is not Product selected) return;
            try { 
                await _api.DeleteProductAsync(selected.Id); 
                await LoadProducts(); 
            } catch { }
        }

        private async void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            if (CartList.Count == 0) return;

            var paymentWindow = new PaymentWindow();
            var dialogResult = await paymentWindow.ShowDialog<double?>(this);

            if (dialogResult.HasValue) // Si le client a validé et payé
            {
                var itemsList = new List<object>();
                foreach (var item in CartList)
                    itemsList.Add(new { product_id = item.Id, qty = item.Quantite });

                var transaction = new { items = itemsList, amount_given = dialogResult.Value };

                try
                {
                    var result = await _api.PostTransactionAsync(transaction);
                    
                    var receiptWindow = new ReceiptWindow(result, dialogResult.Value);
                    await receiptWindow.ShowDialog(this);

                    CartList.Clear();
                    LblTotalTtc.Text = "Total TTC : 0.00 €";
                    await LoadProducts(); await LoadStats();
                }
                catch { }
            }
        }
    }
}