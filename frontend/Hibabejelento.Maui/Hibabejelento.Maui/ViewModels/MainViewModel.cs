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

    [ObservableProperty] private string status = "Ismeretlen";
    [ObservableProperty] private string database = "-";
    [ObservableProperty] private string version = "-";
    [ObservableProperty] private DateTime lastCheck;
    [ObservableProperty] private Color statusColor = Colors.Gray;
    [ObservableProperty] private List<Ticket> tickets = new();
    [ObservableProperty] private string userName = "";

    public MainViewModel(IApiService api) => _api = api;

    [RelayCommand]
    private async Task LoadTicketsAsync()
    {
        userName = await SecureStorage.GetAsync("userName");
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            Tickets = (await _api.GetTicketsAsync()).ToList();
        }
        finally
        {
            IsBusy = false;
        }
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
        //await Shell.Current.GoToAsync(nameof(LoginPage));
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
        
        Shell.Current.GoToAsync(nameof(TicketDetailsPage), parameters);        
    }

    [RelayCommand]
    private async Task Logout()
    {

        SecureStorage.Remove("userName");
        SecureStorage.Remove("userEmail");
        SecureStorage.Remove("userToken");
        SecureStorage.Remove("userExp");
        await Shell.Current.GoToAsync(nameof(LoginPage));
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
