using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Projet6_Avalonia
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double PriceHT { get; set; }
        public double VatRate { get; set; }
        public int Stock { get; set; }
        public string? ImageName { get; set; }

        public Bitmap? ProductImage
        {
            get
            {
                if (string.IsNullOrEmpty(ImageName)) return null;
                try
                {
                    var uri = new Uri($"avares://Projet6_Avalonia/Assets/Images/{ImageName}");
                    return new Bitmap(AssetLoader.Open(uri));
                }
                catch
                {
                    return null;
                }
            }
        }

        // Affichage prix HT simple (pour Gestion des Stocks)
        public string PriceDisplay => $"{PriceHT:F2} €";
        public string VatDisplay => $"{VatRate * 100:0} %";

        // Affichage Point de Vente : TTC en grand, HT en petit, TVA%
        public double PriceTTC => Math.Round(PriceHT * (1 + VatRate), 2);
        public string PriceTTCDisplay => $"{PriceTTC:F2} €";
        public string PriceHTSmallDisplay => $"HT: {PriceHT:F2} €";
        public string VatPercentDisplay => $"TVA {VatRate * 100:0}%";
    }

    public class CartItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        
        private int _quantite;
        public int Quantite 
        { 
            get => _quantite; 
            set 
            { 
                _quantite = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(LineTotalDisplay));
            } 
        }
        
        public double PrixUnit { get; set; }
        public double TVA { get; set; }

        // Prix unitaire TTC
        public double PrixUnitTTC => Math.Round(PrixUnit * (1 + TVA), 2);
        public string PrixUnitDisplay => $"{PrixUnitTTC:F2} €";
        public string LineTotalDisplay => $"{(PrixUnitTTC * Quantite):F2} €";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}