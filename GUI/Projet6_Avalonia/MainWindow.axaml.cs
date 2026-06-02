using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Projet6_Avalonia
{
    public partial class MainWindow : Window
    {
        private ApiService _api;
        public ObservableCollection<Product> ProductsList { get; set; } = new();
        public ObservableCollection<CartItem> CartList { get; set; } = new();

        // Track selected product ID for update vs add logic
        private int? _selectedProductId = null;

        public MainWindow()
        {
            InitializeComponent();
            _api = new ApiService();
            
            DgvPosProducts.ItemsSource = ProductsList;
            DgvCatalogue.ItemsSource = ProductsList;
            CartItemsControl.ItemsSource = CartList;

            // Clic sur un produit dans le catalogue = ajouter au panier directement
            DgvPosProducts.SelectionChanged += DgvPosProducts_SelectionChanged;

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            await LoadProducts();
            await LoadStatsAsync();
        }

        // ===========================================
        // CHARGEMENT PRODUITS
        // ===========================================
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

        // ===========================================
        // POINT DE VENTE — Ajout au panier par clic
        // ===========================================
        private async void DgvPosProducts_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DgvPosProducts.SelectedItem is not Product selected) return;

            // Chercher si déjà dans le panier
            var existing = CartList.FirstOrDefault(c => c.Id == selected.Id);
            if (existing != null)
            {
                existing.Quantite++;
            }
            else
            {
                CartList.Add(new CartItem
                {
                    Id = selected.Id,
                    Nom = selected.Name,
                    Quantite = 1,
                    PrixUnit = selected.PriceHT,
                    TVA = selected.VatRate
                });
            }

            // Désélectionner pour permettre de re-cliquer le même produit
            DgvPosProducts.SelectedItem = null;

            await UpdateTotalFromApi();
        }

        // ===========================================
        // PANIER — Contrôles de quantité +/−
        // ===========================================
        private async void BtnPlus_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                var item = CartList.FirstOrDefault(c => c.Id == productId);
                if (item != null)
                {
                    item.Quantite++;
                    await UpdateTotalFromApi();
                }
            }
        }

        private async void BtnMinus_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                var item = CartList.FirstOrDefault(c => c.Id == productId);
                if (item != null)
                {
                    item.Quantite--;
                    if (item.Quantite <= 0)
                        CartList.Remove(item);
                    await UpdateTotalFromApi();
                }
            }
        }

        private async Task UpdateTotalFromApi()
        {
            try 
            {
                if (CartList.Count == 0)
                {
                    LblTotalTtc.Text = "0.00 €";
                    return;
                }
                var total = await _api.CalculateTotalAsync(CartList);
                LblTotalTtc.Text = $"{total:F2} €";
            }
            catch { LblTotalTtc.Text = "Erreur calcul"; }
        }

        // ===========================================
        // GESTION DES STOCKS — Sélection + Update/Add
        // ===========================================
        private void DgvCatalogue_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DgvCatalogue.SelectedItem is Product selected)
            {
                _selectedProductId = selected.Id;
                TxtName.Text = selected.Name;
                TxtPrice.Text = selected.PriceHT.ToString(CultureInfo.InvariantCulture);
                TxtStock.Text = selected.Stock.ToString();
                
                // Sélectionner le bon taux de TVA
                int vatIndex = selected.VatRate switch
                {
                    0.06 => 0,
                    0.12 => 1,
                    _ => 2
                };
                CbVat.SelectedIndex = vatIndex;

                BtnSave.Content = "💾  Mettre à jour";
                LblEditMode.Text = $"Modification du produit #{selected.Id} — {selected.Name}";
            }
        }

        private async void BtnSaveProduct_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var selectedVat = (CbVat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "0.21";
                var productData = new
                {
                    name = TxtName.Text,
                    price_ht = Convert.ToDouble(TxtPrice.Text.Replace(",", "."), CultureInfo.InvariantCulture),
                    vat_rate = Convert.ToDouble(selectedVat, CultureInfo.InvariantCulture),
                    stock = Convert.ToInt32(TxtStock.Text)
                };

                if (_selectedProductId.HasValue)
                {
                    // MISE À JOUR du produit existant
                    await _api.UpdateProductAsync(_selectedProductId.Value, productData);
                }
                else
                {
                    // AJOUT d'un nouveau produit
                    await _api.AddProductAsync(productData);
                }

                await LoadProducts();
                ClearProductForm();
            }
            catch (Exception ex) 
            { 
                await ShowError($"Erreur de sauvegarde : {ex.Message}");
            }
        }

        private void BtnNewProduct_Click(object? sender, RoutedEventArgs e)
        {
            ClearProductForm();
            DgvCatalogue.SelectedItem = null;
        }

        private void ClearProductForm()
        {
            _selectedProductId = null;
            TxtName.Text = "";
            TxtPrice.Text = "";
            TxtStock.Text = "";
            CbVat.SelectedIndex = 2;
            BtnSave.Content = "💾  Sauvegarder";
            LblEditMode.Text = "Nouveau produit — remplissez les champs ci-dessous";
        }

        private async void BtnDelete_Click(object? sender, RoutedEventArgs e)
        {
            if (DgvCatalogue.SelectedItem is not Product selected) return;
            try 
            { 
                await _api.DeleteProductAsync(selected.Id); 
                await LoadProducts();
                ClearProductForm();
            } 
            catch (Exception ex) 
            { 
                await ShowError($"Erreur lors de la suppression : {ex.Message}");
            }
        }

        // ===========================================
        // PAIEMENT
        // ===========================================
        private async void BtnPay_Click(object? sender, RoutedEventArgs e)
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
                    var result = await _api.PostTransactionAsync(transaction);
                    
                    var receiptWindow = new ReceiptWindow(result, dialogResult.Value);
                    await receiptWindow.ShowDialog(this);

                    CartList.Clear();
                    LblTotalTtc.Text = "0.00 €";
                    await LoadProducts();
                    await LoadStatsAsync(); // Rafraîchir les stats après une vente
                }
                catch (Exception ex) 
                { 
                    await ShowError($"Erreur de transaction : {ex.Message}");
                }
            }
        }

        // ===========================================
        // STATISTIQUES — Chargement inline
        // ===========================================
        private async Task LoadStatsAsync()
        {
            try
            {
                // --- Aujourd'hui ---
                var today = await _api.GetDailyStatsAsync();
                LblDayCa.Text = $"{today.GetProperty("ca_ttc").GetDouble():F2} €";
                LblDayCaHt.Text = $"{today.GetProperty("ca_ht").GetDouble():F2} €";
                LblDayTva.Text = $"{today.GetProperty("tva_total").GetDouble():F2} €";
                LblDayTickets.Text = today.GetProperty("n_tickets").GetInt32().ToString();

                // --- Cette semaine (7 derniers jours) ---
                double weekCa = 0;
                int weekTickets = 0;
                for (int i = 0; i < 7; i++)
                {
                    var dateStr = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
                    var dayStats = await _api.GetDailyStatsAsync(dateStr);
                    weekCa += dayStats.GetProperty("ca_ttc").GetDouble();
                    weekTickets += dayStats.GetProperty("n_tickets").GetInt32();
                }
                LblWeekCa.Text = $"{weekCa:F2} €";
                LblWeekTickets.Text = weekTickets.ToString();

                // --- Ce mois (30 derniers jours) ---
                double monthCa = weekCa;
                int monthTickets = weekTickets;
                for (int i = 7; i < 30; i++)
                {
                    var dateStr = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
                    var dayStats = await _api.GetDailyStatsAsync(dateStr);
                    monthCa += dayStats.GetProperty("ca_ttc").GetDouble();
                    monthTickets += dayStats.GetProperty("n_tickets").GetInt32();
                }
                LblMonthCa.Text = $"{monthCa:F2} €";
                LblMonthTickets.Text = monthTickets.ToString();

                LblStatsError.Text = "";
            }
            catch (Exception ex)
            {
                LblStatsError.Text = $"Erreur de connexion : {ex.Message}";
            }
        }

        // ===========================================
        // UTILITAIRE — Affichage d'erreur
        // ===========================================
        private async Task ShowError(string message)
        {
            var errorWindow = new Window
            {
                Title = "Erreur",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Avalonia.Media.Brushes.White,
                CanResize = false
            };

            var panel = new StackPanel
            {
                Margin = new Avalonia.Thickness(25),
                Spacing = 15,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                FontSize = 14,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E74C3C"))
            });

            var btn = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Padding = new Avalonia.Thickness(30, 8),
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6347")),
                Foreground = Avalonia.Media.Brushes.White,
                FontWeight = Avalonia.Media.FontWeight.Bold
            };
            btn.Click += (_, _) => errorWindow.Close();
            panel.Children.Add(btn);

            errorWindow.Content = panel;
            await errorWindow.ShowDialog(this);
        }
    }
}