using DynamicFormsApp.Shared;
using DynamicFormsApp.Shared.Models;
using System.Net.Http.Json;

namespace DynamicFormsApp.Client.Services
{
    public class ComponentServiceProxy
    {
        private readonly HttpClient _http;
        public ComponentServiceProxy(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<CustomComponent>> GetForUser(string department, string user)
        {
            if (string.IsNullOrEmpty(department) && string.IsNullOrEmpty(user)) return new List<CustomComponent>();
            var result = await _http.GetFromJsonAsync<List<CustomComponent>>($"api/components/{Uri.EscapeDataString(department)}?user={Uri.EscapeDataString(user)}");
            return result ?? new List<CustomComponent>();
        }

        public async Task<CustomComponent?> Save(string name, DesignerField field, string department, string user)
        {
            var resp = await _http.PostAsJsonAsync("api/components", new { Name = name, Field = field, Department = department, CreatedBy = user });
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<CustomComponent>();
        }

        public async Task Delete(int id, string user)
        {
            await _http.DeleteAsync($"api/components/{id}?user={Uri.EscapeDataString(user)}");
        }
    }
}
