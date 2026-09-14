using System.Net.Http.Json;
using Hibabejelento.Maui.Helpers;
using Hibabejelento.Maui.Models;

namespace Hibabejelento.Maui.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(ApiConfig.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<HealthStatus?> GetHealthAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<HealthStatus>("api/health");
        }
        catch
        {
            return null;
        }
    }
}