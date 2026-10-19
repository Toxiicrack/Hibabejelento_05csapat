using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Services;
using Hibabejelento.Maui.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.ViewModels
{
    public partial class LoginViewModel: BaseViewModel
    {
        private readonly IApiService _api;
        [ObservableProperty] public string userEmail;
        [ObservableProperty] public string userPwd;
        [ObservableProperty] public string userError = string.Empty;

        public LoginViewModel(IApiService api)
        {
            _api = api;
        }

        [RelayCommand]
        public async Task Login()
        {
            if (string.IsNullOrWhiteSpace(userEmail) || !userEmail.Contains('@'))
            {
                userError = "Hibás email cím!";
                return;
            }

            if (string.IsNullOrWhiteSpace(userPwd))
            {
                userError = "Kérem adja meg a jelszavát!";
                return;
            }
            string success = await _api.Login(userEmail,userPwd);
            if (string.IsNullOrEmpty(success))
            {
                await Shell.Current.GoToAsync("///MainPage");
            }
            else
            {
                UserError = success;
            }
        }
    }
}
