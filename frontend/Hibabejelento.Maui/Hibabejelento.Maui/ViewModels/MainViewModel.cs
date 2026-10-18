using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Helpers;
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

    public MainViewModel(IApiService api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task CreateTicket()
    {

    }

    [RelayCommand]
    private async Task Filter()
    {

    }

    [RelayCommand]
    private async Task GlobalSearch()
    {

    }
    //[RelayCommand]
    //private async Task NewPage()
    //{
    //    Console.WriteLine("alma");
    //    await Shell.Current.GoToAsync(nameof(NewPage1));
    //}

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