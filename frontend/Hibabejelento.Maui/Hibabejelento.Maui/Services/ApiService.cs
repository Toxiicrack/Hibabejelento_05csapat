using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hibabejelento.Maui.Helpers;
using Hibabejelento.Maui.Models;

namespace Hibabejelento.Maui.Services;

public sealed class ApiService : IApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress ??= new Uri(ApiConfig.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<HealthStatus?> GetHealthAsync()
    {
        try
        {
            using var response = await _http.GetAsync("api/health");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<HealthStatus>();
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }
    }

    public async Task<string> Login(string email, string password)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(
                "api/auth/login", new { email, password });

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return "Helytelen email cím vagy jelszó!";

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return $"A bejelentkezés sikertelen (HTTP {(int)response.StatusCode}).";

            var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(
                body, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result?.User is null || string.IsNullOrWhiteSpace(result.AccessToken))
                return "A szerver hibás bejelentkezési választ küldött.";

            await SecureStorage.SetAsync("userName", result.User.Name ?? string.Empty);
            await SecureStorage.SetAsync("userEmail", result.User.Email ?? email);
            await SecureStorage.SetAsync("userToken", result.AccessToken);
            await SecureStorage.SetAsync("userExp", result.ExpiresAtUtc.ToString());
            return string.Empty;
        }
        catch (HttpRequestException)
        {
            return $"Nem érhető el a szerver: {ApiConfig.BaseUrl}";
        }
        catch (TaskCanceledException)
        {
            return "A szerver nem válaszolt időben.";
        }
        catch (Exception)
        {
            return "Hiba történt a bejelentkezés során.";
        }
    }

    public async Task<bool> CreateTicket(TicketCreation newTicket)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Post, "api/tickets");
            request.Content = JsonContent.Create(newTicket);
            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    public async Task<Ticket?> GetTicketAsync(int ticketId)
    {
        if (ticketId <= 0) return null;

        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Get, $"api/tickets/{ticketId}");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Ticket>();
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public async Task<IEnumerable<Ticket>> GetTicketsAsync()
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Get, "api/tickets");

            System.Diagnostics.Debug.WriteLine(
                $"Request URL: {_http.BaseAddress}{request.RequestUri}");

            System.Diagnostics.Debug.WriteLine(
                $"Authorization Scheme: {request.Headers.Authorization?.Scheme}");

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead
            );

            System.Diagnostics.Debug.WriteLine(
                $"Status: {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"URL: {response.RequestMessage?.RequestUri}");

            System.Diagnostics.Debug.WriteLine(
                $"Authorization: {response.RequestMessage?.Headers.Authorization?.Scheme}");

            System.Diagnostics.Debug.WriteLine(
                $"Redirect: {response.Headers.Location}");
            System.Diagnostics.Debug.WriteLine(
                $"WWW-Authenticate: {string.Join(", ", response.Headers.WwwAuthenticate)}");

            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"Response Body: {responseBody}");

            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<Ticket>();

            return System.Text.Json.JsonSerializer
                .Deserialize<List<Ticket>>(responseBody,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Ticket>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"GetTicketsAsync ERROR: {ex}");

            return Enumerable.Empty<Ticket>();
        }
    }

    private static async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method, string relativeUrl)
    {
        var token = (await SecureStorage.GetAsync("userToken"))?.Trim();

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("A bejelentkezési token hiányzik.");

        var request = new HttpRequestMessage(method, relativeUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }
}
