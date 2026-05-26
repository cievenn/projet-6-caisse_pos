using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Projet6_Avalonia
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double PriceHT { get; set; }
        public double VatRate { get; set; }
        public int Stock { get; set; }

        public string PriceDisplay => $"{PriceHT:F2} €";
        public string VatDisplay => $"{VatRate * 100:0} %";
    }

    public class CartItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        
        private int _quantite;
        public int Quantite 
        { 
            get => _quantite; 
            set { _quantite = value; OnPropertyChanged(); } 
        }
        
        public double PrixUnit { get; set; }
        public double TVA { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}