using Hibabejelento.Maui.ViewModels;
using Hibabejelento.Maui.Views;

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
            if (string.IsNullOrWhiteSpace(token))
            {
                _redirecting = true;
                await Shell.Current.GoToAsync($"//home");
                return;
            }

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
