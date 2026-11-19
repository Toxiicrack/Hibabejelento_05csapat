using System.Globalization;
using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Content.BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var token = await SecureStorage.GetAsync("userToken");
            var expStr = await SecureStorage.GetAsync("userExp");
            if (!string.IsNullOrWhiteSpace(token) &&
                !string.IsNullOrWhiteSpace(expStr) &&
                DateTime.TryParse(expStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var check) &&
                DateTime.UtcNow < check.ToUniversalTime())
            {
                await Shell.Current.GoToAsync(nameof(MainPage));
            }
        }
        catch
        {
            // Ignore malformed stored session values and stay on login screen
        }
    }
}
