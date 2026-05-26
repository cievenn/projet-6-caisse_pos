using Avalonia.Controls;
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

            TxtTicket.Text = ticketText;
        }
    }
}