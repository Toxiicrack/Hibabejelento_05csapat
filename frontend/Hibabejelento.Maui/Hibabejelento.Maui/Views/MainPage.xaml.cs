using System.Globalization;
using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;
    private bool _redirecting;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_redirecting) return;

        try
        {
            var token = await SecureStorage.GetAsync("userToken");
            var expStr = await SecureStorage.GetAsync("userExp");
            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(expStr) ||
                !DateTime.TryParse(expStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var check) ||
                DateTime.UtcNow >= check.ToUniversalTime())
            {
                _redirecting = true;
                await Shell.Current.GoToAsync("//home");
                return;
            }

            LoginButton.IsVisible = false;
            LoggedInGrid.IsVisible = true;
            await _vm.CheckHealthCommand.ExecuteAsync(null);
            await _vm.LoadTicketsCommand.ExecuteAsync(null);
        }
        catch (Exception)
        {
            _redirecting = true;
            await Shell.Current.GoToAsync("//home");
        }
    }
}
