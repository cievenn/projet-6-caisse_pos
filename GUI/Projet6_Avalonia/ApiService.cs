using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Projet6_Avalonia
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService()
        {
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        }

        public async Task<JsonElement> GetProductsAsync()
        {
            var response = await _client.GetAsync("/api/products");
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(jsonString).RootElement.GetProperty("data");
        }

        public async Task<JsonElement> PostTransactionAsync(object transactionBody)
        {
            var json = JsonSerializer.Serialize(transactionBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/transactions", content);
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);

            if (!response.IsSuccessStatusCode) throw new Exception(jsonDoc.RootElement.GetProperty("message").GetString());
            return jsonDoc.RootElement.GetProperty("data");
        }

        public async Task AddProductAsync(object productBody)
        {
            var json = JsonSerializer.Serialize(productBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/products", content);
            if (!response.IsSuccessStatusCode) throw new Exception("Erreur lors de l'ajout du produit.");
        }
        
        public async Task UpdateProductAsync(int id, object productBody)
        {
            var json = JsonSerializer.Serialize(productBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"/api/products/{id}", content);
            if (!response.IsSuccessStatusCode) throw new Exception("Erreur lors de la modification du produit.");
        }

        public async Task DeleteProductAsync(int id)
        {
            var response = await _client.DeleteAsync($"/api/products/{id}");
            if (!response.IsSuccessStatusCode) throw new Exception("Impossible de supprimer (le stock doit être à 0).");
        }

        public async Task<JsonElement> GetDailyStatsAsync()
        {
            var response = await _client.GetAsync("/api/stats/daily");
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(jsonString).RootElement.GetProperty("data");
        }
    }
}