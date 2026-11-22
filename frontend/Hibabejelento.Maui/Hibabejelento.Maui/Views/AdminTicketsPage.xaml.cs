using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class AdminTicketsPage : ContentPage
{
    private readonly AdminTicketsViewModel _vm;

    public AdminTicketsPage(AdminTicketsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCurrentTabAsync();
    }
}
