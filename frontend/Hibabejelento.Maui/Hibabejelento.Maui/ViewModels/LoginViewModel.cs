using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Services;
using Hibabejelento.Maui.Views;

namespace Hibabejelento.Maui.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IApiService _api;
        [ObservableProperty] public string userEmail = string.Empty;
        [ObservableProperty] public string userPwd = string.Empty;
        [ObservableProperty] public string userError = string.Empty;

        public LoginViewModel(IApiService api)
        {
            _api = api;
        }

        [RelayCommand]
        public async Task Login()
        {
            UserError = string.Empty;

            if (string.IsNullOrWhiteSpace(UserEmail) || !UserEmail.Contains('@'))
            {
                UserError = "Hibás email cím!";
                return;
            }

            if (string.IsNullOrWhiteSpace(UserPwd))
            {
                UserError = "Kérem adja meg a jelszavát!";
                return;
            }

            string error = await _api.Login(UserEmail, UserPwd);
            if (string.IsNullOrEmpty(error))
            {
                await Shell.Current.GoToAsync(nameof(MainPage));
            }
            else
            {
                UserError = error;
            }
        }
    }
}
