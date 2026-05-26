using Avalonia;
using System;

namespace Projet6_Avalonia
{
    class Program
    {
        // C'est ici que l'application s'allume !
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Configuration du moteur Avalonia
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}