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

        private async void BtnAddCart_Click(object sender, RoutedEventArgs e)
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
            
            // Délégation stricte du calcul à l'API
            await UpdateTotalFromApi();
        }

        private async Task UpdateTotalFromApi()
        {
            try 
            {
                // Envoie le panier à l'API pour qu'elle calcule le total via la DLL C
                var total = await _api.CalculateTotalAsync(CartList);
                LblTotalTtc.Text = $"TOTAL : {total:F2} €";
            }
            catch { LblTotalTtc.Text = "TOTAL : Erreur calcul"; }
        }

        private void DgvCatalogue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgvCatalogue.SelectedItem is Product selected)
            {
                TxtName.Text = selected.Name;
                TxtPrice.Text = selected.PriceHT.ToString(CultureInfo.InvariantCulture);
                TxtStock.Text = selected.Stock.ToString();
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

            if (dialogResult.HasValue) 
            {
                var itemsList = new List<object>();
                foreach (var item in CartList)
                    itemsList.Add(new { product_id = item.Id, qty = item.Quantite });

                var transaction = new { items = itemsList, amount_given = dialogResult.Value };

                try
                {
                    // L'API fait la transaction, met à jour la DB et calcule le rendu monnaie via le C
                    var result = await _api.PostTransactionAsync(transaction);
                    
                    CartList.Clear();
                    LblTotalTtc.Text = "TOTAL : 0.00 €";
                    await LoadProducts();
                }
                catch { }
            }
        }
    }
}