using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Hibabejelento.Maui.Helpers;
using Hibabejelento.Maui.Models;

namespace Hibabejelento.Maui.Services;

public sealed class ApiService : IApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

            return await response.Content.ReadFromJsonAsync<HealthStatus>(JsonOptions);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
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

            var result = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);

            if (result?.User is null || string.IsNullOrWhiteSpace(result.AccessToken))
                return "A szerver hibás bejelentkezési választ küldött.";

            await SecureStorage.SetAsync("userId", result.User.Id.ToString());
            await SecureStorage.SetAsync("userName", result.User.Name ?? string.Empty);
            await SecureStorage.SetAsync("userEmail", result.User.Email ?? email);
            await SecureStorage.SetAsync("userToken", result.AccessToken);
            await SecureStorage.SetAsync("userExp", result.ExpiresAtUtc.ToString("O"));
            await SecureStorage.SetAsync("userIsAdmin", result.User.IsAdmin.ToString());            
            if (result.User.GroupId != null)
                await SecureStorage.SetAsync("userGroupId", result.User.GroupId.Value.ToString());
            else
                SecureStorage.Remove("userGroupId");

            if (!string.IsNullOrWhiteSpace(result.User.GroupName))
                await SecureStorage.SetAsync("userGroupName", result.User.GroupName);
            else
                SecureStorage.Remove("userGroupName");

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
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

    public async Task<Ticket?> GetTicketAsync(string ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber)) return null;

        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Get, $"api/tickets/{Uri.EscapeDataString(ticketNumber)}");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Ticket>(JsonOptions);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public async Task<IEnumerable<Ticket>> GetMyTicketsAsync()
        => await GetTicketsFromEndpointAsync("api/tickets/mine");

    public async Task<IEnumerable<Ticket>> GetMaintenanceTicketsAsync()
        => await GetTicketsFromEndpointAsync("api/tickets/maintenance");

    private async Task<IEnumerable<Ticket>> GetTicketsFromEndpointAsync(string endpoint)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, endpoint);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<Ticket>();

            return JsonSerializer.Deserialize<List<Ticket>>(responseBody, JsonOptions) ?? new List<Ticket>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTicketsFromEndpointAsync ({endpoint}) ERROR: {ex}");
            return Enumerable.Empty<Ticket>();
        }
    }

    public async Task<List<Category>?> GetCategoriesAsync()
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Get, "api/categories");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<List<Category>>(JsonOptions);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public async Task<List<Models.Location>?> GetLocationsAsync()
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Get, "api/locations");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<List<Models.Location>>(JsonOptions);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public async Task<bool> WithdrawTicketAsync(int ticketId)
        => await PostAsync($"api/tickets/{ticketId}/withdraw", (object?)null);

    public async Task<List<TicketNote>> GetTicketNotesAsync(int ticketId)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"api/tickets/{ticketId}/notes");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();
            return await response.Content.ReadFromJsonAsync<List<TicketNote>>(JsonOptions) ?? new();
        }
        catch { return new(); }
    }

    public async Task<bool> AddTicketNoteAsync(int ticketId, string message)
        => await PostAsync($"api/tickets/{ticketId}/notes", new TicketNoteRequest { Message = message });

    public async Task<bool> ReopenTicketAsync(int ticketId, string reason)
        => await PostAsync($"api/tickets/{ticketId}/reopen", new TicketReopening { Reason = reason });

    public async Task<bool> TakeTicketAsync(int ticketId)
        => await PostAsync($"api/tickets/{ticketId}/take", (object?)null);

    public async Task<bool> CloseTicketAsync(int ticketId, int closureTypeId, string resolution)
        => await PostAsync($"api/tickets/{ticketId}/close", new TicketCloseRequest { ClosureTypeId = closureTypeId, Resolution = resolution });

    public async Task<List<TicketStatusHistory>> GetTicketStatusHistoryAsync(int ticketId)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"api/tickets/{ticketId}/status-history");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();
            return await response.Content.ReadFromJsonAsync<List<TicketStatusHistory>>(JsonOptions) ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<Notification>> GetNotificationsAsync(bool unreadOnly = false, int take = 50)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"api/notifications?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}&take={take}");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();
            return await response.Content.ReadFromJsonAsync<List<Notification>>(JsonOptions) ?? new();
        }
        catch { return new(); }
    }

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, "api/notifications/unread-count");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return 0;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Number) return doc.RootElement.GetInt32();
            foreach (var key in new[] { "count", "unreadCount" })
                if (doc.RootElement.TryGetProperty(key, out var value)) return value.GetInt32();
            return 0;
        }
        catch { return 0; }
    }

    public async Task<bool> MarkNotificationReadAsync(int notificationId)
        => await PostAsync($"api/notifications/{notificationId}/read", (object?)null);

    public async Task<bool> MarkAllNotificationsReadAsync()
        => await PostAsync("api/notifications/read-all", (object?)null);

    // Admin endpoints
    public async Task<IEnumerable<Ticket>> GetAdminTicketsAsync(bool newOnly = false)
        => await GetTicketsFromEndpointAsync($"api/admin/tickets?newOnly={newOnly.ToString().ToLowerInvariant()}");

    public async Task<bool> AcceptTicketAsync(int ticketId, AcceptTicketRequest request)
        => await PostAsync($"api/admin/tickets/{ticketId}/accept", request);

    // Admin - Users
    public async Task<List<UserDto>?> GetAdminUsersAsync(bool pendingOnly = false)
        => await GetJsonAsync<List<UserDto>>($"api/admin/users?pendingOnly={pendingOnly.ToString().ToLowerInvariant()}");

    public async Task<bool> CreateUserAsync(string name, string email, string password, string roleType, int? groupId)
    {
        bool isAdmin = string.Equals(roleType, "Admin", StringComparison.OrdinalIgnoreCase);
        int? gid = string.Equals(roleType, "Karbantartó", StringComparison.OrdinalIgnoreCase) ? groupId : null;
        return await PostAsync("api/admin/users", new
        {
            name,
            email,
            password,
            isAdmin,
            groupId = gid
        });
    }

    public async Task<bool> ApproveUserAsync(int userId)
        => await PostAsync($"api/admin/users/{userId}/approve", (object?)null);

    public async Task<bool> DeactivateUserAsync(int userId)
        => await PatchAsync($"api/admin/users/{userId}/deactivate");

    public async Task<bool> AssignUserGroupAsync(int userId, int? groupId)
        => await PutAsync($"api/admin/users/{userId}/group", new { groupId });

    // Admin - Groups
    public async Task<List<Group>?> GetAdminGroupsAsync()
        => await GetJsonAsync<List<Group>>("api/admin/groups");

    public async Task<bool> CreateGroupAsync(string name, string? description)
        => await PostAsync("api/admin/groups", new { name, description });

    public async Task<bool> UpdateGroupAsync(int id, string name, string? description)
        => await PutAsync($"api/admin/groups/{id}", new { name, description });

    public async Task<bool> DeactivateGroupAsync(int id)
        => await PatchAsync($"api/admin/groups/{id}/deactivate");

    // Admin - Categories
    public async Task<List<Category>?> GetAdminCategoriesAsync()
        => await GetJsonAsync<List<Category>>("api/admin/categories");

    public async Task<bool> CreateCategoryAsync(string name, string? description)
        => await PostAsync("api/admin/categories", new { name, description });

    public async Task<bool> DeactivateCategoryAsync(int id)
        => await PatchAsync($"api/admin/categories/{id}/deactivate");

    // Admin - Locations
    public async Task<List<Models.Location>?> GetAdminLocationsAsync()
        => await GetJsonAsync<List<Models.Location>>("api/admin/locations");

    public async Task<bool> CreateLocationAsync(string name, string? description)
        => await PostAsync("api/admin/locations", new { name, description });

    public async Task<bool> UpdateLocationAsync(int id, string name, string? description)
        => await PutAsync($"api/admin/locations/{id}", new { name, description });

    public async Task<bool> DeactivateLocationAsync(int id)
        => await PatchAsync($"api/admin/locations/{id}/deactivate");

    // Admin - Priorities
    public async Task<List<Priority>?> GetAdminPrioritiesAsync()
        => await GetJsonAsync<List<Priority>>("api/admin/priorities");

    public async Task<bool> CreatePriorityAsync(string name, int defaultDueDays, bool isDefault)
        => await PostAsync("api/admin/priorities", new { name, defaultDueDays, isDefault });

    public async Task<bool> UpdatePriorityAsync(int id, string name, int defaultDueDays, bool isDefault)
        => await PutAsync($"api/admin/priorities/{id}", new { name, defaultDueDays, isDefault });

    public async Task<bool> DeactivatePriorityAsync(int id)
        => await PatchAsync($"api/admin/priorities/{id}/deactivate");

    private async Task<bool> PostAsync<T>(string url, T? body)
        => await SendAuthorizedAsync(HttpMethod.Post, url, body);

    private async Task<bool> PutAsync<T>(string url, T? body)
        => await SendAuthorizedAsync(HttpMethod.Put, url, body);

    private async Task<bool> PatchAsync(string url)
        => await SendAuthorizedAsync<object?>(HttpMethod.Patch, url, null);

    private async Task<bool> SendAuthorizedAsync<T>(HttpMethod method, string url, T? body)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(method, url);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private async Task<T?> GetJsonAsync<T>(string url)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch { return default; }
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
