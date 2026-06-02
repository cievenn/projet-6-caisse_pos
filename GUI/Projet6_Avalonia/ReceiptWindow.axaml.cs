using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Text.Json;

namespace Projet6_Avalonia
{
    public partial class ReceiptWindow : Window
    {
        public ReceiptWindow() { InitializeComponent(); } // Requis par le designer

        public ReceiptWindow(JsonElement apiResult, double amountGiven)
        {
            InitializeComponent();

            double totalTtc = apiResult.GetProperty("total_ttc").GetDouble();
            int transId = apiResult.GetProperty("transaction_id").GetInt32();

            // Header info
            LblDate.Text = DateTime.Now.ToString("dd/MM/yyyy à HH:mm");
            LblTransId.Text = $"Transaction N°{transId}";

            // Totaux
            LblTotal.Text = $"{totalTtc:F2} €";
            LblPaid.Text = $"{amountGiven:F2} €";
            LblChange.Text = $"{(amountGiven - totalTtc):F2} €";

            // Détail du rendu monnaie
            var change = apiResult.GetProperty("change_returned");
            foreach (var coin in change.EnumerateObject())
            {
                int count = coin.Value.GetInt32();
                if (count > 0)
                {
                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                    row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                    var lblName = new TextBlock
                    {
                        Text = $"{coin.Name} €",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };

                    var lblCount = new TextBlock
                    {
                        Text = $"× {count}",
                        FontSize = 11,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Color.Parse("#64748B")),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };

                    Grid.SetColumn(lblName, 0);
                    Grid.SetColumn(lblCount, 1);
                    row.Children.Add(lblName);
                    row.Children.Add(lblCount);

                    PanelChangeDetail.Children.Add(row);
                }
            }
        }
    }
}