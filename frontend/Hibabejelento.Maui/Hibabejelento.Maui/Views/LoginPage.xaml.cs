using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class LoginPage : ContentPage
{
	private LoginViewModel _vm;
	public LoginPage(LoginViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		Content.BindingContext = _vm;
		
	}

    protected override async void OnAppearing()
    {
		DateTime check = Convert.ToDateTime(await SecureStorage.GetAsync("userExp"));
		
        if (DateTime.UtcNow < check){
			await Shell.Current.GoToAsync(nameof(MainPage));
		}
    }
}