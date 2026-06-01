using System;
using System.Collections.Generic;
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

        // Délégué au backend (qui utilise le C via Python)
        public async Task<double> CalculateTotalAsync(IEnumerable<CartItem> cart)
        {
            var itemsList = new List<object>();
            foreach (var item in cart)
                itemsList.Add(new { product_id = item.Id, qty = item.Quantite });

            var json = JsonSerializer.Serialize(new { items = itemsList });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // On suppose ici que tu as créé une route /api/transactions/calculate dans Flask
            var response = await _client.PostAsync("/api/transactions/calculate", content);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(jsonString).RootElement.GetProperty("data").GetProperty("total_ttc").GetDouble();
            }
            return 0;
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
            if (!response.IsSuccessStatusCode) throw new Exception("Erreur d'ajout.");
        }

        public async Task DeleteProductAsync(int id)
        {
            var response = await _client.DeleteAsync($"/api/products/{id}");
            if (!response.IsSuccessStatusCode) throw new Exception("Impossible de supprimer.");
        }
    }
}