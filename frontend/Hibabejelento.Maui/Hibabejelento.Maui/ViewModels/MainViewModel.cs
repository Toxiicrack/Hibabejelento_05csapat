using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Helpers;
using Hibabejelento.Maui.Models;
using Hibabejelento.Maui.Services;
using Hibabejelento.Maui.Views;
using Hibabejelento.Maui.WinUI;

namespace Hibabejelento.Maui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IApiService _api;

    [ObservableProperty] private string status = "Ismeretlen";
    [ObservableProperty] private string database = "-";
    [ObservableProperty] private string version = "-";
    [ObservableProperty] private DateTime lastCheck;
    [ObservableProperty] private Color statusColor = Colors.Gray;

    [ObservableProperty] private List<Ticket> tickets;

    public MainViewModel(IApiService api)
    {
        _api = api;
        GetTickets();
    }

    private async void GetTickets()
    {
        tickets = (await _api.GetTicketsAsync()).ToList();
        Console.WriteLine("alma");
    }

    [RelayCommand]
    private async Task Filter()
    {

    }

    [RelayCommand]
    private async Task GlobalSearch()
    {

    }
    [RelayCommand]
    private async Task CreateTicket()
    {        
        await Shell.Current.GoToAsync(nameof(TicketCreationPage));
    }

    [RelayCommand]
    private async Task Login()
    {
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
                StatusColor = result.Status == "Healthy" ? Colors.Green : Colors.Orange;
            }

            LastCheck = DateTime.Now;
        }
        finally
        {
            IsBusy = false;
        }
    }
}