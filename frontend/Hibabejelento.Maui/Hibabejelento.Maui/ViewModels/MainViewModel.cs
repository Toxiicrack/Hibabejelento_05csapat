using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Helpers;
using Hibabejelento.Maui.Models;
using Hibabejelento.Maui.Services;
using Hibabejelento.Maui.Views;

namespace Hibabejelento.Maui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IApiService _api;
    private List<Ticket> _allTickets = new();

    [ObservableProperty] private string status = "Ismeretlen";
    [ObservableProperty] private string database = "-";
    [ObservableProperty] private string version = "-";
    [ObservableProperty] private DateTime lastCheck;
    [ObservableProperty] private Color statusColor = Colors.Gray;
    [ObservableProperty] private List<Ticket> tickets = new();
    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string userInitials = string.Empty;
    [ObservableProperty] private int unreadNotificationCount;
    [ObservableProperty] private bool hasUnreadNotifications;

    [ObservableProperty] private bool isMaintenanceUser;
    [ObservableProperty] private bool isMaintenanceMode;
    [ObservableProperty] private bool isNotificationsOpen;
    [ObservableProperty] private List<Notification> notifications = new();

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedStatus = "Összes";

    public string ActiveViewLabel => IsMaintenanceMode ? "Karbantartói csoport hibajegyei" : "Saját hibajegyek";
    public string ToggleModeButtonText => IsMaintenanceMode ? "Saját jegyek" : "Karbantartói nézet";

    public List<string> StatusOptions { get; } = new()
    {
        "Összes", "Új", "Folyamatban", "VálaszraVár", "Lezárt", "Újranyitva", "Visszavonva"
    };

    public MainViewModel(IApiService api) => _api = api;

    partial void OnIsMaintenanceModeChanged(bool value)
    {
        OnPropertyChanged(nameof(ActiveViewLabel));
        OnPropertyChanged(nameof(ToggleModeButtonText));
    }

    [RelayCommand]
    private async Task LoadTicketsAsync()
    {
        UserName = await SecureStorage.GetAsync("userName") ?? string.Empty;
        UserInitials = string.Join("", UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => char.ToUpperInvariant(x[0])));

        var groupIdStr = await SecureStorage.GetAsync("userGroupId");
        IsMaintenanceUser = int.TryParse(groupIdStr, out var gid) && gid > 0;
        if (!IsMaintenanceUser)
            IsMaintenanceMode = false;

        await RefreshNotificationsAsync();

        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = IsMaintenanceMode
                ? await _api.GetMaintenanceTicketsAsync()
                : await _api.GetMyTicketsAsync();
            _allTickets = result.ToList();
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleTicketViewModeAsync()
    {
        if (!IsMaintenanceUser) return;
        IsMaintenanceMode = !IsMaintenanceMode;
        await LoadTicketsAsync();
    }

    [RelayCommand]
    private async Task ToggleNotificationsPanelAsync()
    {
        IsNotificationsOpen = !IsNotificationsOpen;
        if (IsNotificationsOpen)
            await RefreshNotificationsAsync();
    }

    [RelayCommand]
    private async Task RefreshNotificationsAsync()
    {
        UnreadNotificationCount = await _api.GetUnreadNotificationCountAsync();
        HasUnreadNotifications = UnreadNotificationCount > 0;
        if (IsNotificationsOpen)
        {
            Notifications = await _api.GetNotificationsAsync(false, 50);
        }
    }

    [RelayCommand]
    private async Task MarkNotificationReadAsync(Notification? notification)
    {
        if (notification is null || notification.IsRead) return;
        if (await _api.MarkNotificationReadAsync(notification.Id))
        {
            await RefreshNotificationsAsync();
        }
    }

    [RelayCommand]
    private async Task MarkAllNotificationsReadAsync()
    {
        if (await _api.MarkAllNotificationsReadAsync())
        {
            await RefreshNotificationsAsync();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedStatusChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _allTickets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedStatus) && SelectedStatus != "Összes")
        {
            filtered = filtered.Where(t => string.Equals(t.Status, SelectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.Title?.ToLowerInvariant().Contains(q) ?? false) ||
                (t.TicketNumber?.ToLowerInvariant().Contains(q) ?? false) ||
                (t.Location?.ToLowerInvariant().Contains(q) ?? false) ||
                (t.CategoryName?.ToLowerInvariant().Contains(q) ?? false) ||
                (t.AssetIdentifier?.ToLowerInvariant().Contains(q) ?? false));
        }

        Tickets = filtered.ToList();
    }

    [RelayCommand]
    private Task Filter() => Task.CompletedTask;

    [RelayCommand]
    private Task GlobalSearch() => Task.CompletedTask;

    [RelayCommand]
    private async Task CreateTicket()
    {
        await Shell.Current.GoToAsync(nameof(TicketCreationPage));
    }

    [RelayCommand]
    private async Task Login()
    {
        await Shell.Current.GoToAsync("//home");
    }

    [ObservableProperty]
    private Ticket? selectedTicket;

    partial void OnSelectedTicketChanged(Ticket? value)
    {
        if (value == null) return;

        var parameters = new Dictionary<string, object>
        {
            { "SelectedTicket", value }
        };

        SelectedTicket = null;
        if (IsMaintenanceMode)
            Shell.Current.GoToAsync(nameof(TicketEditPage), parameters);
        else
            Shell.Current.GoToAsync(nameof(TicketDetailsPage), parameters);
    }

    [RelayCommand]
    private async Task Logout()
    {
        SecureStorage.Remove("userId");
        SecureStorage.Remove("userName");
        SecureStorage.Remove("userEmail");
        SecureStorage.Remove("userToken");
        SecureStorage.Remove("userExp");
        SecureStorage.Remove("userIsAdmin");
        SecureStorage.Remove("userGroupId");
        SecureStorage.Remove("userGroupName");
        await Shell.Current.GoToAsync("//home");
    }

    [RelayCommand]
    private async Task CheckHealthAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _api.GetHealthAsync();
            if (result is null)
            {
                Status = "Elérhetetlen";
                Database = "-";
                Version = "-";
                StatusColor = Colors.Red;
                ErrorMessage = $"Nem sikerült elérni a backendet ({ApiConfig.BaseUrl}).";
            }
            else
            {
                Status = result.Status;
                Database = result.Database;
                Version = result.Version;
                StatusColor = string.Equals(result.Status, "Healthy",
                    StringComparison.OrdinalIgnoreCase) ? Colors.Green : Colors.Orange;
            }

            LastCheck = DateTime.Now;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
