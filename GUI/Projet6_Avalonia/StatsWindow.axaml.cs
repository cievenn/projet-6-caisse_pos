using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace Projet6_Avalonia
{
    public partial class StatsWindow : Window
    {
        private readonly ApiService _api;

        public StatsWindow()
        {
            InitializeComponent();
            _api = new ApiService();
            _ = LoadStatsAsync();
        }

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
                double monthCa = weekCa; // on a déjà les 7 premiers jours
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
            }
            catch (Exception ex)
            {
                LblError.Text = $"Erreur de connexion : {ex.Message}";
            }
        }
    }
}
