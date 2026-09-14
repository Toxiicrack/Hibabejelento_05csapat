using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CheckHealthCommand.ExecuteAsync(null);
    }
}