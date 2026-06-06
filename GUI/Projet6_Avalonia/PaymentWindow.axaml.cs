using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Globalization;

namespace Projet6_Avalonia
{
    public partial class PaymentWindow : Window
    {
        public PaymentWindow() => InitializeComponent();

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            try {
                double amount = Convert.ToDouble(TxtAmount.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                Close(amount);
            } catch { }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close(null);
    }
}