using ABI.System;
using Duende.IdentityModel.Client;
using Hibabejelento.Maui.Helpers;
using Hibabejelento.Maui.Models;
using Hibabejelento.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Hibabejelento.Maui.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new System.Uri(ApiConfig.BaseUrl);
        _http.Timeout = System.TimeSpan.FromSeconds(10);
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

    public async Task<string> Login(string email, string password)
    {        
        var response = await _http.PostAsJsonAsync("api/auth/login", new { email = email, password = password });
        string result = await response.Content.ReadAsStringAsync();

        if ((int)response.StatusCode == 401)
        {
            return "Helytelen bejelentkezés!";
        }
        else
        {
            var loginResult = JsonConvert.DeserializeObject<LoginResponse>(result);
            
            string token = loginResult.AccessToken;
            string userName = loginResult.User.Name;
            bool isAdmin = loginResult.User.IsAdmin;
            await SecureStorage.SetAsync("userName",userName);
            await SecureStorage.SetAsync("userEmail", loginResult.User.Email);            
            await SecureStorage.SetAsync("userToken", token);

            return string.Empty;

        }
    }

    public async Task<bool> CreateTicket(TicketCreation newTicket)
    {
        var token = await SecureStorage.GetAsync("userToken");
        try
        {
            string cleanToken = token.Trim('"');
            _http.SetBearerToken(cleanToken);            
            var response = await _http.PostAsJsonAsync("api/tickets", newTicket);            
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {            
            return false;
        }
        catch (TaskCanceledException)
        {         
            return false;
        }
    }

    public async Task<Ticket?> GetTicketAsync(int ticketId)
    {
        Ticket ticket = new Ticket();
        return ticket;
    }

    public async Task<IEnumerable<Ticket>> GetTicketsAsync()
    {
        var token = await SecureStorage.GetAsync("userToken");
        try
        {
            string cleanToken = token.Trim('"');
            _http.SetBearerToken(cleanToken);
            return await _http.GetFromJsonAsync<List<Ticket>>(
                "api/tickets")
                ?? new List<Ticket>();            
        }
        catch (HttpRequestException)
        {            
            return Enumerable.Empty<Ticket>();
        }
        catch (TaskCanceledException)
        {
            return Enumerable.Empty<Ticket>();
        }
    }
}